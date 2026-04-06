namespace DigitalHearth.Api.DTOs.Task;

public record UpdateTaskRequest(string? Name, int? IntervalDays, bool? IsOneTime = null);
