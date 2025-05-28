namespace DevHabit.Api.DTOs.GithubAccessTokens;

public record StoreGithubAccessTokenDto()
{
    public string AccessToken { get; init; }
    public int ExpiresInDays { get; set; } = 5;
}