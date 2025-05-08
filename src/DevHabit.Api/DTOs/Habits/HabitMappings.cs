using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;

namespace DevHabit.Api.DTOs.Habits;

internal static class HabitMappings
{
    public static readonly SortMappingDefinition<HabitDto, Habit> SortMapping = new()
    {
        Mappings =
        [
            new SortMapping(nameof(HabitDto.Name), nameof(Habit.Name)),
            new SortMapping(nameof(HabitDto.Description), nameof(Habit.Description)),
            new SortMapping(nameof(HabitDto.CreatedAtUtc), nameof(Habit.CreatedAtUtc)),
            new SortMapping(nameof(HabitDto.UpdatedAtUtc), nameof(Habit.UpdatedAtUtc)),
            new SortMapping(nameof(HabitDto.LastCompletedAtUtc), nameof(Habit.LastCompletedAtUtc)),
            new SortMapping(nameof(HabitDto.EndDate), nameof(Habit.EndDate)),
            new SortMapping(nameof(HabitDto.Status), nameof(Habit.Status)),
            new SortMapping(nameof(HabitDto.Type), nameof(Habit.Type)),
            new SortMapping(
                $"{nameof(HabitDto.Frequency)}.{nameof(FrequencyDto.Type)}",
                $"{nameof(Habit.Frequency)}.{nameof(Habit.Frequency.Type)}"),
            new SortMapping(
                $"{nameof(HabitDto.Frequency)}.{nameof(FrequencyDto.TimesPerPeriod)}",
                $"{nameof(Habit.Frequency)}.{nameof(Habit.Frequency.TimesPerPeriod)}"),
            new SortMapping(
                $"{nameof(HabitDto.Target)}.{nameof(TargetDto.Value)}",
                $"{nameof(Habit.Target)}.{nameof(Habit.Target.Value)}"),
            new SortMapping(
                $"{nameof(HabitDto.Target)}.{nameof(TargetDto.Unit)}",
                $"{nameof(Habit.Target)}.{nameof(Habit.Target.Unit)}"),

        ]
    };
    public static HabitDto ToDto(this Habit habit)
    {
        return new HabitDto
        {
            Id = habit.Id,
            Name = habit.Name,
            Description = habit.Description,
            Type = habit.Type,
            Frequency = new FrequencyDto
            {
                Type = habit.Frequency.Type,
                TimesPerPeriod = habit.Frequency.TimesPerPeriod
            },
            Target = new TargetDto
            {
                Value = habit.Target.Value,
                Unit = habit.Target.Unit
            },
            Status = habit.Status,
            IsArchived = habit.IsArchived,
            EndDate = habit.EndDate,
            Milestone = habit.Milestone == null
                ? null
                : new MilestoneDto
                {
                    Target = habit.Milestone.Target,
                    Current = habit.Milestone.Current
                },
            CreatedAtUtc = habit.CreatedAtUtc,
            UpdatedAtUtc = habit.UpdatedAtUtc,
            LastCompletedAtUtc = habit.LastCompletedAtUtc
        };
    }
    public static Habit ToEntity(this CreateHabitDto dto, string userId)
    {
        Habit habit = new()
        {
            Id = $"h_{Guid.CreateVersion7()}",
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            Frequency = new Frequency
            {
                Type = dto.Frequency.Type,
                TimesPerPeriod = dto.Frequency.TimesPerPeriod
            },
            Target = new Target
            {
                Value = dto.Target.Value,
                Unit = dto.Target.Unit
            },
            Status = HabitStatus.Ongoing,
            IsArchived = false,
            EndDate = dto.EndDate,
            Milestone = dto.Milestone is not null
                ? new Milestone
                {
                    Target = dto.Milestone.Target,
                    Current = 0 // Initialize current progress to 0
                }
                : null,
            CreatedAtUtc = DateTime.UtcNow
        };

        return habit;
    }
    
    public static void UpdateFromDto(this Habit habit, UpdateHabitDto updateHabitDto)
    {
        habit.Name = updateHabitDto.Name;
        habit.Description = updateHabitDto.Description;
        habit.Type = updateHabitDto.Type;
        habit.EndDate = updateHabitDto.EndDate;

        habit.Frequency = new Frequency
        {
            Type = updateHabitDto.Frequency.Type, 
            TimesPerPeriod = updateHabitDto.Frequency.TimesPerPeriod,
        };
        
        habit.Target = new Target
        {
            Value = updateHabitDto.Target.Value,
            Unit = updateHabitDto.Target.Unit,
        };

        if (updateHabitDto.Milestone is not null)
        {
            habit.Milestone ??= new Milestone();
            habit.Milestone.Target = updateHabitDto.Milestone.Target;
        }

        habit.UpdatedAtUtc = DateTime.UtcNow;
    }
}