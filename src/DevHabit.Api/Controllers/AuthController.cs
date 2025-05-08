using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Auth;
using DevHabit.Api.DTOs.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public sealed class AuthController(
    UserManager<IdentityUser> userManager, 
    ApplicationIdentityDbContext identityDbContext,
    ApplicationDbContext applicationDbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> options) : ControllerBase
{
    private readonly JwtAuthOptions _jwtAuthOptions = options.Value;
    
    [HttpPost("register")]
    public async Task<ActionResult<AccessTokensDto>> Register(RegisterUserDto registerUserDto)
    {
        using IDbContextTransaction transaction = await identityDbContext.Database.BeginTransactionAsync();
        applicationDbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());
        // Create Identity user
        IdentityUser identityUser = new IdentityUser
        {
            Email = registerUserDto.Email, 
            UserName = registerUserDto.Name,
        };
        
        IdentityResult identityResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);
        if (!identityResult.Succeeded)
        {
            Dictionary<string, object?> extensions = new Dictionary<string, object?> 
            {
                
                {   "errors",
                    identityResult.Errors.ToDictionary(e => e.Code, e => e.Description)
                }
            };
            return Problem(
                detail: "Unable to register user, please try again.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions:  extensions);
        }
        // Create App user
        User user = registerUserDto.ToEntity();
        user.IdentityId = identityUser.Id;
        applicationDbContext.Users.Add(user);
        await applicationDbContext.SaveChangesAsync();

        

        TokenRequest tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email!);
        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays),
        };
        
        identityDbContext.RefreshTokens.Add(refreshToken);
        await identityDbContext.SaveChangesAsync();
        
        await transaction.CommitAsync();
        
        return Ok(accessTokens);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccessTokensDto>> Login(LoginUserDto loginUserDto)
    {
        IdentityUser? identityUser = await userManager.FindByEmailAsync(loginUserDto.Email);
        
        if(identityUser is null || !await userManager.CheckPasswordAsync(identityUser,loginUserDto.Password))
        {
                return Unauthorized();
        }

        TokenRequest tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email!);
        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);
        
        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessTokens.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays),
        };
        
        identityDbContext.RefreshTokens.Add(refreshToken);
        await identityDbContext.SaveChangesAsync();
        
        return Ok(accessTokens);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AccessTokensDto>> Refresh(RefreshTokenDto refreshTokenDto)
    {
        RefreshToken? refreshToken =
            await identityDbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(e => e.Token == refreshTokenDto.RefreshToken);
        if (refreshToken is null)
        {
            return Unauthorized();
        }

        if (refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }
        
        TokenRequest tokenRequest = new TokenRequest(refreshToken.User.Id, refreshToken.User.Email!);
        AccessTokensDto accessTokens = tokenProvider.Create(tokenRequest);

        refreshToken.Token = accessTokens.RefreshToken;
        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationInDays);
        
        await identityDbContext.SaveChangesAsync();

        return Ok(accessTokens);
    }
}