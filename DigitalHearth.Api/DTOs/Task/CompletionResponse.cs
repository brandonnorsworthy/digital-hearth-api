namespace DigitalHearth.Api.DTOs.Task;

public record CompletionResponse(int Id, int TaskId, DateTime CompletedAt, int UserId, string Username);
