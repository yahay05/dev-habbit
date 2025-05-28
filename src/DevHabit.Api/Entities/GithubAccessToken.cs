namespace DevHabit.Api.Entities;

public sealed class GithubAccessToken
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}