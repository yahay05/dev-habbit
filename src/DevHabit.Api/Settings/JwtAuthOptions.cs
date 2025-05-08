namespace DevHabit.Api.Settings;

public sealed class JwtAuthOptions
{
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public string Key { get; init; }
    public int ExpirationInMinutes { get; init; }
    public int RefreshTokenExpirationInDays { get; init; }
}