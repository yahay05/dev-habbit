using Asp.Versioning;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Dynamic;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ApiVersion(1.0)]
public sealed class HabitsController(ApplicationDbContext dbContext, LinkService linkService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHabits([FromQuery]
        HabitQueryParameters query,
        SortMappingProvider sortMappingProvider,
        DataShapingService dataShapingService)
    {
        if (!sortMappingProvider.ValidateMapping<HabitDto, Habit>(query.Sort))
        {
            return Problem($"The provided sorting parameter isn't valid: '{query.Sort}'", statusCode: StatusCodes.Status400BadRequest);
        }

        if(!dataShapingService.ValidateFields<HabitDto>(query.Fields))
        {
            return Problem($"The provided data shaping fields aren't valid: '{query.Fields}'", statusCode: StatusCodes.Status400BadRequest);
        }
        query.Search ??= query.Search?.Trim().ToLower();

        SortMapping[] sortMappings = sortMappingProvider.GetMappings<HabitDto, Habit>();


        IQueryable<HabitDto> habitsQuery = dbContext
            .Habits
            .Where(h => query.Search == null ||
                        h.Name.ToLower().Contains(query.Search) ||
                        h.Description != null && h.Description.ToLower().Contains(query.Search))
            .Where(h => query.Type == null || h.Type == query.Type)
            .Where(h => query.Status == null || h.Status == query.Status)
            .ApplySort(query.Sort, sortMappings)
            .Select(HabitQueries.ProjectToDto());
        
        int totalCount = await habitsQuery.CountAsync(); 
        List<HabitDto> habits = await habitsQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();
        bool includeLinks = query.Accept == CustomMediaTypeNames.Application.HateoasJson;
        PaginationResult<ExpandoObject> paginationResult = new PaginationResult<ExpandoObject>
        {
            Items = dataShapingService.ShapeCollectionData(
                habits,
                query.Fields,
                includeLinks ? h => CreateLinksForHabit(h.Id, query.Fields) : null),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
        if(includeLinks)
            paginationResult.Links = CreateLinksForHabits(query, paginationResult.HasNextPage, paginationResult.HasPreviousPage);
        
        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    [MapToApiVersion(1)]
    public async Task<IActionResult> GetHabit(
        string id,
        string? fields,
        [FromHeader(Name = "Accept")]
        string? accept,
        DataShapingService dataShapingService)
    {
        if (!dataShapingService.ValidateFields<HabitWithTagsDto>(fields))
        {
            return Problem($"The provided data shaping fields aren't valid: '{fields}'",
                statusCode: StatusCodes.Status400BadRequest);
        }

        HabitWithTagsDto? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToHabitWithTagsDto())
            .FirstOrDefaultAsync();

        if (habit is null)
            return NotFound();

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, fields);
        
        if (accept == CustomMediaTypeNames.Application.HateoasJson)
        {
            List<LinkDto> links = CreateLinksForHabit(id, fields);
            shapedHabitDto.TryAdd("links", links);
        }
        
        return Ok(shapedHabitDto);
    }
    
    [HttpGet("{id}")]
    [ApiVersion(2)]
    public async Task<IActionResult> GetHabitV2(
        string id,
        string? fields,
        [FromHeader(Name = "Accept")]
        string? accept,
        DataShapingService dataShapingService)
    {
        if (!dataShapingService.ValidateFields<HabitWithTagsDtoV2>(fields))
        {
            return Problem($"The provided data shaping fields aren't valid: '{fields}'",
                statusCode: StatusCodes.Status400BadRequest);
        }

        HabitWithTagsDtoV2? habit = await dbContext
            .Habits
            .Where(h => h.Id == id)
            .Select(HabitQueries.ProjectToHabitWithTagsDtoV2())
            .FirstOrDefaultAsync();

        if (habit is null)
            return NotFound();

        ExpandoObject shapedHabitDto = dataShapingService.ShapeData(habit, fields);
        
        if (accept == CustomMediaTypeNames.Application.HateoasJson)
        {
            List<LinkDto> links = CreateLinksForHabit(id, fields);
            shapedHabitDto.TryAdd("links", links);
        }
        
        return Ok(shapedHabitDto);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator
    )
    {
        await validator.ValidateAndThrowAsync(createHabitDto);
        
        Habit habit = createHabitDto.ToEntity();
        
        dbContext.Habits.Add(habit);

        await dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();
        habitDto.Links = CreateLinksForHabit(habit.Id, null);

        return CreatedAtAction(nameof(GetHabit), new { id = habit.Id }, habitDto);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateHabit(
        string id,
        [FromBody] UpdateHabitDto updateHabitDto,
        IValidator<UpdateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(updateHabitDto);
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null)
        {
            return NotFound();
        }
        habit.UpdateFromDto(updateHabitDto);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchHabit(string id, [FromBody] JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null)
        {
            return NotFound();
        }

        HabitDto habitDto = habit.ToDto();
        patchDocument.ApplyTo(habitDto, ModelState);

        if (!TryValidateModel(habitDto))
        {
            return ValidationProblem(ModelState);
        }
        
        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteHabit(string id)
    {
        Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(h => h.Id == id);
        if (habit is null)
        {
            // return StatusCode(StatusCodes.Status410Gone);
            return NotFound();
        }

        dbContext.Habits.Remove(habit);
        await dbContext.SaveChangesAsync();
        
        return NoContent();
    }
    
    private List<LinkDto> CreateLinksForHabit(string id, string? fields)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabit), "self", HttpMethods.Get, new { id, fields }),
            linkService.Create(nameof(UpdateHabit), "update", HttpMethods.Put, new { id }),
            linkService.Create(nameof(PatchHabit), "partial-update", HttpMethods.Patch, new { id }),
            linkService.Create(nameof(DeleteHabit), "delete", HttpMethods.Delete, new { id }),
            linkService.Create(
                nameof(HabitTagsController.UpsertHabitTags),
                "upsert-tags",
                HttpMethods.Put,
                new { habitId = id },
                HabitTagsController.Name
                ),
        ];
        return links;
    }

    private List<LinkDto> CreateLinksForHabits(
        HabitQueryParameters parameters,
        bool hasNextPage,
        bool hasPreviousPage)
    {
        List<LinkDto> links =
        [
            linkService.Create(nameof(GetHabits), "self", HttpMethods.Get, new
            {
                page = parameters.Page,
                pageSize = parameters.PageSize,
                sort = parameters.Sort,
                q = parameters.Search,
                type = parameters.Type,
                status = parameters.Status,
                fields = parameters.Fields,
            }),
            linkService.Create(nameof(CreateHabit), "create", HttpMethods.Post),
        ];
        if (hasNextPage)
            links.Add(linkService.Create(nameof(GetHabits), "next-page", HttpMethods.Get, new
            {
                page = parameters.Page + 1,
                pageSize = parameters.PageSize,
                sort = parameters.Sort,
                q = parameters.Search,
                type = parameters.Type,
                status = parameters.Status,
                fields = parameters.Fields,
            }));
        if (hasPreviousPage)
            links.Add(linkService.Create(nameof(GetHabits), "previous-page", HttpMethods.Get, new
            {
                page = parameters.Page - 1,
                pageSize = parameters.PageSize,
                sort = parameters.Sort,
                q = parameters.Search,
                type = parameters.Type,
                status = parameters.Status,
                fields = parameters.Fields,
            }));
        return links;   
    }
    
}