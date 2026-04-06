namespace DigitalHearth.Api.DTOs.Task;

public record CreateTaskRequest(string Name, int IntervalDays, bool IsOneTime = false);
