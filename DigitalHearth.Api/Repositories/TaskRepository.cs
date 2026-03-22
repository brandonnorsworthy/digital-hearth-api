using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public async Task<List<RecurringTask>> GetByHouseholdAsync(int householdId, CancellationToken ct)
    {
        return await db.RecurringTasks
            .Where(t => t.HouseholdId == householdId)
            .Include(t => t.LastCompletedByUser)
            .ToListAsync(ct);
    }

    public async Task<RecurringTask?> GetByIdWithUserAsync(int id, CancellationToken ct)
    {
        return await db.RecurringTasks
            .Include(t => t.LastCompletedByUser)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<RecurringTask?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await db.RecurringTasks.FindAsync([id], ct);
    }

    public async Task<RecurringTask> CreateAsync(RecurringTask task, CancellationToken ct)
    {
        db.RecurringTasks.Add(task);
        await db.SaveChangesAsync(ct);
        return task;
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(RecurringTask task, CancellationToken ct)
    {
        db.RecurringTasks.Remove(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task<TaskCompletion> AddCompletionAsync(TaskCompletion completion, CancellationToken ct)
    {
        db.TaskCompletions.Add(completion);
        await db.SaveChangesAsync(ct);
        return completion;
    }

    public async Task<List<CompletionResponse>> GetHistoryAsync(int taskId, CancellationToken ct)
    {
        return await db.TaskCompletions
            .Where(c => c.TaskId == taskId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CompletedAt)
            .Select(c => new CompletionResponse(c.Id, c.TaskId, c.CompletedAt, c.UserId, c.User.Username))
            .ToListAsync(ct);
    }

    public async Task<List<RecurringTask>> GetDueInWindowAsync(DateTime windowStart, DateTime now, CancellationToken ct)
    {
        return await db.RecurringTasks
            .Where(t =>
                (t.LastCompletedAt ?? t.CreatedAt).AddDays(t.IntervalDays) >= windowStart &&
                (t.LastCompletedAt ?? t.CreatedAt).AddDays(t.IntervalDays) < now)
            .Include(t => t.NotifPreferences)
            .Include(t => t.Household).ThenInclude(h => h.Members)
            .ToListAsync(ct);
    }
}
