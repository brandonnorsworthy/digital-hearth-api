namespace DigitalHearth.Api.DTOs.Task;

public record CreateTaskRequest(string Name, string Tier, int IntervalDays);
