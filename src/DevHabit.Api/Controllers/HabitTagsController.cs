using DevHabit.Api.Database;
using DevHabit.Api.DTOs.HabitTags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[Authorize]
[ApiController]
[Route("habits/{habitId}/tags")]
public sealed class HabitTagsController(
    ApplicationDbContext dbContext,
    UserContext userContext) : ControllerBase
{
    public static readonly string Name = nameof(HabitTagsController).Replace("Controller", string.Empty);
    [HttpPut]
    public async Task<ActionResult> UpsertHabitTags(string habitId, UpsertHabitTagsDto habitTagsDto)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await dbContext.Habits
            .Include(h => h.HabitTags)
            .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

        if (habit is null)
        {
            return NotFound();
        }

        var currentTagIds = habit.HabitTags.Select(ht => ht.TagId).ToHashSet();
        if (currentTagIds.SetEquals(habitTagsDto.TagIds))
        {
            return NoContent();
        }
        
        List<string> existingTagIds = await dbContext
            .Tags
            .Where(t => habitTagsDto.TagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        if (existingTagIds.Count != habitTagsDto.TagIds.Count)
        {
            return BadRequest("One or more tag IDs is invalid");
        }
        
        habit.HabitTags.RemoveAll(ht => !habitTagsDto.TagIds.Contains(ht.TagId));
        
        string[] tagIdsToAdd = habitTagsDto.TagIds.Except(currentTagIds).ToArray();
        habit.HabitTags.AddRange(tagIdsToAdd.Select(tagId => new HabitTag
        {
            TagId = tagId,
            HabitId = habitId,
            CreatedAtUtc = DateTime.UtcNow
        }));
        
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
    
    [HttpDelete("{tagId}")]
    public async Task<ActionResult> DeleteHabitTag(string habitId, string tagId)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }
        
        HabitTag? habitTag = await dbContext
            .HabitTags
            .SingleOrDefaultAsync(ht => ht.HabitId == habitId && ht.TagId == tagId);
        
        
        if (habitTag is null)
        {
            return NotFound();
        }
        
        Habit? habit = await dbContext
            .Habits
            .FirstOrDefaultAsync(h => h.Id == habitTag.HabitId && h.UserId == userId);

        if (habit is null)
        {
            return NotFound();
        }
        
        dbContext.HabitTags.Remove(habitTag);
        await dbContext.SaveChangesAsync();
        return NoContent(); 
    }
}