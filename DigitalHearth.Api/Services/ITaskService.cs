using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface ITaskService
{
    Task<ServiceResult<IReadOnlyList<TaskResponse>>> ListAsync(Guid householdId, User user, CancellationToken ct = default);
    Task<ServiceResult<TaskResponse>> CreateAsync(Guid householdId, CreateTaskRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult<TaskResponse>> UpdateAsync(Guid id, UpdateTaskRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(Guid id, User user, CancellationToken ct = default);
    Task<ServiceResult<TaskResponse>> CompleteAsync(Guid id, User user, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<CompletionResponse>>> GetHistoryAsync(Guid id, User user, CancellationToken ct = default);
}
