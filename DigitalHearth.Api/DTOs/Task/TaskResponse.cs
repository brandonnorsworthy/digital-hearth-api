namespace DigitalHearth.Api.DTOs.Task;

public record TaskResponse(
    Guid Id,
    Guid HouseholdId,
    string Name,
    int IntervalDays,
    DateTime? LastCompletedAt,
    string? LastCompletedBy,
    DateTime NextDueAt);
