using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;

namespace DigitalHearth.Api.Services;

public class TaskService(ITaskRepository tasks) : ITaskService
{
    private static TaskResponse ToResponse(RecurringTask t) => new(
        t.Id,
        t.HouseholdId,
        t.Name,
        t.IntervalDays,
        t.LastCompletedAt,
        t.LastCompletedByUser?.Username,
        (t.LastCompletedAt ?? t.CreatedAt).AddDays(t.IntervalDays));

    public async Task<ServiceResult<IReadOnlyList<TaskResponse>>> ListAsync(
        Guid householdId, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<IReadOnlyList<TaskResponse>>.Forbidden();

        var list = await tasks.GetByHouseholdAsync(householdId, ct);

        var responses = list
            .Select(ToResponse)
            .OrderBy(t => t.NextDueAt)
            .ToList();

        return ServiceResult<IReadOnlyList<TaskResponse>>.Ok(responses);
    }

    public async Task<ServiceResult<TaskResponse>> CreateAsync(
        Guid householdId, CreateTaskRequest req, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<TaskResponse>.Forbidden();

        if (req.IntervalDays <= 0)
            return ServiceResult<TaskResponse>.BadRequest("IntervalDays must be greater than 0");

        var task = new RecurringTask
        {
            HouseholdId = householdId,
            Name = req.Name,
            IntervalDays = req.IntervalDays,
            CreatedAt = DateTime.UtcNow
        };

        await tasks.CreateAsync(task, ct);

        return ServiceResult<TaskResponse>.Ok(ToResponse(task));
    }

    public async Task<ServiceResult<TaskResponse>> UpdateAsync(
        Guid id, UpdateTaskRequest req, User user, CancellationToken ct = default)
    {
        var task = await tasks.GetByIdWithUserAsync(id, ct);

        if (task is null)
            return ServiceResult<TaskResponse>.NotFound("Task not found");
        if (task.HouseholdId != user.HouseholdId)
            return ServiceResult<TaskResponse>.Forbidden();

        if (req.Name is not null) task.Name = req.Name;
        if (req.IntervalDays is not null)
        {
            if (req.IntervalDays <= 0)
                return ServiceResult<TaskResponse>.BadRequest("IntervalDays must be greater than 0");
            task.IntervalDays = req.IntervalDays.Value;
        }

        await tasks.SaveAsync(ct);
        return ServiceResult<TaskResponse>.Ok(ToResponse(task));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, User user, CancellationToken ct = default)
    {
        var task = await tasks.GetByIdAsync(id, ct);
        if (task is null)
            return ServiceResult.NotFound("Task not found");
        if (task.HouseholdId != user.HouseholdId)
            return ServiceResult.Forbidden();

        await tasks.DeleteAsync(task, ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<TaskResponse>> CompleteAsync(Guid id, User user, CancellationToken ct = default)
    {
        var task = await tasks.GetByIdWithUserAsync(id, ct);

        if (task is null)
            return ServiceResult<TaskResponse>.NotFound("Task not found");
        if (task.HouseholdId != user.HouseholdId)
            return ServiceResult<TaskResponse>.Forbidden();

        var now = DateTime.UtcNow;
        task.LastCompletedAt = now;
        task.LastCompletedByUserId = user.Id;
        task.LastCompletedByUser = user;

        await tasks.AddCompletionAsync(new TaskCompletion
        {
            TaskId = id,
            UserId = user.Id,
            CompletedAt = now
        }, ct);

        return ServiceResult<TaskResponse>.Ok(ToResponse(task));
    }

    public async Task<ServiceResult<IReadOnlyList<CompletionResponse>>> GetHistoryAsync(
        Guid id, User user, CancellationToken ct = default)
    {
        var task = await tasks.GetByIdAsync(id, ct);
        if (task is null)
            return ServiceResult<IReadOnlyList<CompletionResponse>>.NotFound("Task not found");
        if (task.HouseholdId != user.HouseholdId)
            return ServiceResult<IReadOnlyList<CompletionResponse>>.Forbidden();

        var completions = await tasks.GetHistoryAsync(id, ct);

        return ServiceResult<IReadOnlyList<CompletionResponse>>.Ok(completions);
    }
}
