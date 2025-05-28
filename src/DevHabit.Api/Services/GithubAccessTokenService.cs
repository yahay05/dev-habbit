using DevHabit.Api.Database;
using DevHabit.Api.DTOs.GithubAccessTokens;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Services;

public sealed class GithubAccessTokenService(ApplicationDbContext dbContext)
{
    public async Task StoreAsync(
        string userId,
        StoreGithubAccessTokenDto accessTokenDto,
        CancellationToken cancellationToken = default)
    {
        GithubAccessToken? existingAccessToken  = await GetAccessTokenAsync(userId, cancellationToken);
        if(existingAccessToken is not null)
        {
            existingAccessToken.Token = accessTokenDto.AccessToken;
            existingAccessToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays);
        }
        else
        {
            dbContext.GithubAccessTokens.Add(new GithubAccessToken
            {
                Id = $"gh_{Guid.CreateVersion7()}",
                UserId = userId,
                Token = accessTokenDto.AccessToken,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(accessTokenDto.ExpiresInDays),
            });
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        GithubAccessToken? githubAccessToken = await GetAccessTokenAsync(userId, cancellationToken);
        return githubAccessToken?.Token;
    }
    
    public async Task RevokeAsync(string userId, CancellationToken cancellationToken = default)
    {
        GithubAccessToken? githubAccessToken = await GetAccessTokenAsync(userId, cancellationToken);
        if(githubAccessToken is null)
        {
            return;
        }

        dbContext.GithubAccessTokens.Remove(githubAccessToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
    
    private async Task<GithubAccessToken?> GetAccessTokenAsync(string userId, CancellationToken cancellationToken)
    {
        return await dbContext.GithubAccessTokens.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}