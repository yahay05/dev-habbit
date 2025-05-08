using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Users;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DevHabit.Api.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class UsersController(ApplicationDbContext dbContext, UserContext userContext) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (userId != id)
        {
            return Forbid();
        }
        UserDto? userDto = await dbContext.Users
            .Where(u => u.Id == id)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (userDto is null)
        {
            return NotFound();
        }
        
        return Ok(userDto);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        string? userId = await userContext.GetUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }
        
        UserDto? userDto = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        if (userDto is null)
        {
            return NotFound();
        }
        
        return Ok(userDto);
    }
}