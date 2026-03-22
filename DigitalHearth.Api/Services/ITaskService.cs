using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface ITaskService
{
    Task<ServiceResult<IReadOnlyList<TaskResponse>>> ListAsync(int householdId, User user, CancellationToken ct = default);
    Task<ServiceResult<TaskResponse>> CreateAsync(int householdId, CreateTaskRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult<TaskResponse>> UpdateAsync(int id, UpdateTaskRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(int id, User user, CancellationToken ct = default);
    Task<ServiceResult<TaskResponse>> CompleteAsync(int id, User user, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<CompletionResponse>>> GetHistoryAsync(int id, User user, CancellationToken ct = default);
}
