namespace DigitalHearth.Api.DTOs.Task;

public record UpdateTaskRequest(string? Name, string? Tier, int? IntervalDays);
