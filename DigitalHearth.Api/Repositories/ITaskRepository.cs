using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Repositories;

public interface ITaskRepository
{
    Task<List<RecurringTask>> GetByHouseholdAsync(Guid householdId, CancellationToken ct);
    Task<RecurringTask?> GetByIdWithUserAsync(Guid id, CancellationToken ct);
    Task<RecurringTask?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<RecurringTask> CreateAsync(RecurringTask task, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task DeleteAsync(RecurringTask task, CancellationToken ct);
    Task<TaskCompletion> AddCompletionAsync(TaskCompletion completion, CancellationToken ct);
    Task<List<CompletionResponse>> GetHistoryAsync(Guid taskId, CancellationToken ct);
    Task<List<RecurringTask>> GetDueInWindowAsync(DateTime windowStart, DateTime now, CancellationToken ct);
    Task<List<RecurringTask>> GetDueTasksAsync(DateTime now, CancellationToken ct);
}
