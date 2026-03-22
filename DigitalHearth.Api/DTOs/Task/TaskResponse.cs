namespace DigitalHearth.Api.DTOs.Task;

public record TaskResponse(
    int Id,
    int HouseholdId,
    string Name,
    int IntervalDays,
    DateTime? LastCompletedAt,
    string? LastCompletedBy,
    DateTime NextDueAt);
