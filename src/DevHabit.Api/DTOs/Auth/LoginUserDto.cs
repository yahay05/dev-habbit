namespace DevHabit.Api.DTOs.Auth;

public sealed record LoginUserDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}