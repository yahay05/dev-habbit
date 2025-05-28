using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Common;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace DevHabit.Api.Controllers;


[Authorize(Roles = $"{Roles.Member}")]
[ApiController]
[Route("[controller]")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.HateoasJsonV1)]
public sealed class TagsController(
    ApplicationDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
        [HttpGet]
        public async Task<ActionResult<TagsCollectionDto>> GetTags([FromHeader] AcceptHeaderDto acceptHeader)
        {
            string? userId = await userContext.GetUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }
            
            List<TagDto> tags = await dbContext
                .Tags
                .Where(tag => tag.UserId == userId)
                .Select(TagQueries.ProjectToDto())
                .ToListAsync();
    
            TagsCollectionDto tagsCollectionDto = new TagsCollectionDto
            {
                Items = tags
            };

            if (acceptHeader.IncludeLinks)
            {
                tagsCollectionDto.Links = CreateLinksForTags();
            }
    
            return Ok(tagsCollectionDto);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<TagDto>> GetTag(
            string id,
            [FromHeader] AcceptHeaderDto acceptHeader)
        {
            string? userId = await userContext.GetUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }
            
            TagDto? tag = await dbContext
                .Tags
                .Where(tag => tag.Id == id && tag.UserId == userId)
                .Select(TagQueries.ProjectToDto())
                .FirstOrDefaultAsync();

            if (tag is null)
            {
                return NotFound();
            }

            if (acceptHeader.IncludeLinks)
            {
                tag.Links = CreateLinksForTag(id);
            }

            return Ok(tag);
        }
        
        [HttpPost]
        public async Task<ActionResult<TagDto>> CreateTag(
            CreateTagDto createTagDto,
            IValidator<CreateTagDto> validator
            )
        {
            string? userId = await userContext.GetUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }
            
            await validator.ValidateAndThrowAsync(createTagDto);
            
            Tag tag = createTagDto.ToEntity(userId);

            if (await dbContext.Tags.AnyAsync(t => t.Name == tag.Name))
            {
                return Problem(
                    detail: $"The tag '{tag.Name}' already exists",
                    statusCode: StatusCodes.Status409Conflict
                    );
            }

            dbContext.Tags.Add(tag);

            await dbContext.SaveChangesAsync();

            TagDto tagDto = tag.ToDto();

            return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
        }
        
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateTag(string id, UpdateTagDto updateTagDto)
        {
            string? userId = await userContext.GetUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }
            
            Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(tag => tag.Id == id && tag.UserId == userId);

            if (tag is null)
            {
                return NotFound();
            }

            tag.UpdateFromDto(updateTagDto);

            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTag(string id)
        {
            string? userId = await userContext.GetUserIdAsync();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }
            Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(tag => tag.Id == id && tag.UserId == userId);

            if (tag is null)
            {
                return NotFound();
            }

            dbContext.Tags.Remove(tag);

            await dbContext.SaveChangesAsync();

            return NoContent();
        }
        
        private List<LinkDto> CreateLinksForTag(string id)
        {
            List<LinkDto> links =
            [
                linkService.Create(nameof(GetTag), "self", HttpMethods.Get, new { id }),
                linkService.Create(nameof(UpdateTag), "update", HttpMethods.Put, new { id }),
                linkService.Create(nameof(DeleteTag), "delete", HttpMethods.Delete, new { id }),
            ];
            return links;
        }

        private List<LinkDto> CreateLinksForTags()
        {
            List<LinkDto> links =
            [
                linkService.Create(nameof(GetTags), "self", HttpMethods.Get),
                linkService.Create(nameof(CreateTag), "create", HttpMethods.Post),
            ];
            return links;
        }
}