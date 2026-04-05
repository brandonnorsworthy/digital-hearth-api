namespace DigitalHearth.Api.DTOs.Task;

public record CompletionResponse(Guid Id, Guid TaskId, DateTime CompletedAt, Guid UserId, string Username);
