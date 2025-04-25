using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Tags;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class TagsController(ApplicationDbContext dbContext) : ControllerBase
{
        [HttpGet]
        public async Task<ActionResult<TagsCollectionDto>> GetTags()
        {
            List<TagDto> tags = await dbContext
                .Tags
                .Select(TagQueries.ProjectToDto())
                .ToListAsync();
    
            TagsCollectionDto tagsCllCollectionDto = new TagsCollectionDto
            {
                Data = tags
            };
    
            return Ok(tagsCllCollectionDto);
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<TagDto>> GetTag(string id)
        {
            TagDto? tag = await dbContext
                .Tags
                .Where(h => h.Id == id)
                .Select(TagQueries.ProjectToDto())
                .FirstOrDefaultAsync();

            if (tag is null)
            {
                return NotFound();
            }

            return Ok(tag);
        }
        
        [HttpPost]
        public async Task<ActionResult<TagDto>> CreateTag(CreateTagDto createTagDto)
        {
            Tag tag = createTagDto.ToEntity();

            if (await dbContext.Tags.AnyAsync(t => t.Name == tag.Name))
            {
                return Conflict($"The tag '{tag.Name}' already exists");
            }

            dbContext.Tags.Add(tag);

            await dbContext.SaveChangesAsync();

            TagDto tagDto = tag.ToDto();

            return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
        }
        
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateTag(string id, UpdateTagDto updateTagDto)
        {
            Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(h => h.Id == id);

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
            Tag? tag = await dbContext.Tags.FirstOrDefaultAsync(h => h.Id == id);

            if (tag is null)
            {
                return NotFound();
            }

            dbContext.Tags.Remove(tag);

            await dbContext.SaveChangesAsync();

            return NoContent();
        }
}