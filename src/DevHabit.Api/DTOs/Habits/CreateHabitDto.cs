using DevHabit.Api.Entities;

namespace DevHabit.Api.DTOs.Habits;

public sealed record CreateHabitDto
{
    public string Name { get; init; }
    public string? Description { get; init; }
    public HabitType Type { get; init; }
    public FrequencyDto Frequency { get; init; }
    public TargetDto Target { get; init; }
    public DateOnly? EndDate { get; init; }
    public MilestoneDto? Milestone { get; init; }
}