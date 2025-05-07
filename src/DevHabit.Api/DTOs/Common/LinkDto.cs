namespace DevHabit.Api.DTOs.Common;

public sealed class LinkDto
{
    public required string Href { get; init; }
    public string Rel { get; init; }
    public required string Method { get; init; }
}