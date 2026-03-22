namespace DigitalHearth.Api.DTOs.Task;

public record TaskResponse(
    int Id,
    int HouseholdId,
    string Name,
    string Tier,
    int IntervalDays,
    DateTime? LastCompletedAt,
    string? LastCompletedBy,
    DateTime NextDueAt);
