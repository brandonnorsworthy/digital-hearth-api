using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Controllers;

[ApiController]
public class TaskController(AppDbContext db, ICurrentUserService currentUser) : ApiControllerBase
{
    private static readonly string[] ValidTiers = ["short", "medium", "long"];

    private static TaskResponse ToResponse(RecurringTask t) => new(
        t.Id,
        t.HouseholdId,
        t.Name,
        t.Tier,
        t.IntervalDays,
        t.LastCompletedAt,
        t.LastCompletedByUser?.Username,
        (t.LastCompletedAt ?? t.CreatedAt).AddDays(t.IntervalDays));

    [HttpGet("api/households/{householdId:int}/tasks")]
    public async Task<IActionResult> List(int householdId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != householdId)
            return Forbid();

        var tasks = await db.RecurringTasks
            .Where(t => t.HouseholdId == householdId)
            .Include(t => t.LastCompletedByUser)
            .ToListAsync(ct);

        var responses = tasks
            .Select(ToResponse)
            .OrderBy(t => t.NextDueAt)
            .ToList();

        return Ok(responses);
    }

    [HttpPost("api/households/{householdId:int}/tasks")]
    public async Task<IActionResult> Create(int householdId, [FromBody] CreateTaskRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != householdId)
            return Forbid();

        if (!ValidTiers.Contains(req.Tier))
            return BadRequest(new { error = "Tier must be 'short', 'medium', or 'long'" });

        if (req.IntervalDays <= 0)
            return BadRequest(new { error = "IntervalDays must be greater than 0" });

        var task = new RecurringTask
        {
            HouseholdId = householdId,
            Name = req.Name,
            Tier = req.Tier,
            IntervalDays = req.IntervalDays,
            CreatedAt = DateTime.UtcNow
        };

        db.RecurringTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(List), new { householdId }, ToResponse(task));
    }

    [HttpPut("api/tasks/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var task = await db.RecurringTasks
            .Include(t => t.LastCompletedByUser)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (task is null) return NotFound(new { error = "Task not found" });
        if (task.HouseholdId != user!.HouseholdId) return Forbid();

        if (req.Name is not null) task.Name = req.Name;
        if (req.Tier is not null)
        {
            if (!ValidTiers.Contains(req.Tier))
                return BadRequest(new { error = "Tier must be 'short', 'medium', or 'long'" });
            task.Tier = req.Tier;
        }
        if (req.IntervalDays is not null)
        {
            if (req.IntervalDays <= 0)
                return BadRequest(new { error = "IntervalDays must be greater than 0" });
            task.IntervalDays = req.IntervalDays.Value;
        }

        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(task));
    }

    [HttpDelete("api/tasks/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var task = await db.RecurringTasks.FindAsync([id], ct);
        if (task is null) return NotFound(new { error = "Task not found" });
        if (task.HouseholdId != user!.HouseholdId) return Forbid();

        db.RecurringTasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("api/tasks/{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var task = await db.RecurringTasks
            .Include(t => t.LastCompletedByUser)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (task is null) return NotFound(new { error = "Task not found" });
        if (task.HouseholdId != user!.HouseholdId) return Forbid();

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
        return Ok(ToResponse(task));
    }

    [HttpGet("api/tasks/{id:int}/history")]
    public async Task<IActionResult> History(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var task = await db.RecurringTasks.FindAsync([id], ct);
        if (task is null) return NotFound(new { error = "Task not found" });
        if (task.HouseholdId != user!.HouseholdId) return Forbid();

        var completions = await db.TaskCompletions
            .Where(c => c.TaskId == id)
            .Include(c => c.User)
            .OrderByDescending(c => c.CompletedAt)
            .Select(c => new CompletionResponse(c.Id, c.TaskId, c.CompletedAt, c.UserId, c.User.Username))
            .ToListAsync(ct);

        return Ok(completions);
    }
}
