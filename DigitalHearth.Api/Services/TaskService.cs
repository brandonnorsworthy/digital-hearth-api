using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Services;

public class TaskService(AppDbContext db) : ITaskService
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
        int householdId, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<IReadOnlyList<TaskResponse>>.Forbidden();

        var tasks = await db.RecurringTasks
            .Where(t => t.HouseholdId == householdId)
            .Include(t => t.LastCompletedByUser)
            .ToListAsync(ct);

        var responses = tasks
            .Select(ToResponse)
            .OrderBy(t => t.NextDueAt)
            .ToList();

        return ServiceResult<IReadOnlyList<TaskResponse>>.Ok(responses);
    }

    public async Task<ServiceResult<TaskResponse>> CreateAsync(
        int householdId, CreateTaskRequest req, User user, CancellationToken ct = default)
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

        db.RecurringTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return ServiceResult<TaskResponse>.Ok(ToResponse(task));
    }

    public async Task<ServiceResult<TaskResponse>> UpdateAsync(
        int id, UpdateTaskRequest req, User user, CancellationToken ct = default)
    {
        var task = await db.RecurringTasks
            .Include(t => t.LastCompletedByUser)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

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

        await db.SaveChangesAsync(ct);
        return ServiceResult<TaskResponse>.Ok(ToResponse(task));
    }

    public async Task<ServiceResult> DeleteAsync(int id, User user, CancellationToken ct = default)
    {
        var task = await db.RecurringTasks.FindAsync([id], ct);
        if (task is null)
            return ServiceResult.NotFound("Task not found");
        if (task.HouseholdId != user.HouseholdId)
            return ServiceResult.Forbidden();

        db.RecurringTasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<TaskResponse>> CompleteAsync(int id, User user, CancellationToken ct = default)
    {
        var task = await db.RecurringTasks
            .Include(t => t.LastCompletedByUser)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (task is null)
            return ServiceResult<TaskResponse>.NotFound("Task not found");
        if (task.HouseholdId != user.HouseholdId)
            return ServiceResult<TaskResponse>.Forbidden();

        var now = DateTime.UtcNow;
        task.LastCompletedAt = now;
        task.LastCompletedByUserId = user.Id;
        task.LastCompletedByUser = user;

        db.TaskCompletions.Add(new TaskCompletion
        {
            TaskId = id,
            UserId = user.Id,
            CompletedAt = now
        });

        await db.SaveChangesAsync(ct);
        return ServiceResult<TaskResponse>.Ok(ToResponse(task));
    }

    public async Task<ServiceResult<IReadOnlyList<CompletionResponse>>> GetHistoryAsync(
        int id, User user, CancellationToken ct = default)
    {
        var task = await db.RecurringTasks.FindAsync([id], ct);
        if (task is null)
            return ServiceResult<IReadOnlyList<CompletionResponse>>.NotFound("Task not found");
        if (task.HouseholdId != user.HouseholdId)
            return ServiceResult<IReadOnlyList<CompletionResponse>>.Forbidden();

        var completions = await db.TaskCompletions
            .Where(c => c.TaskId == id)
            .Include(c => c.User)
            .OrderByDescending(c => c.CompletedAt)
            .Select(c => new CompletionResponse(c.Id, c.TaskId, c.CompletedAt, c.UserId, c.User.Username))
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<CompletionResponse>>.Ok(completions);
    }
}
