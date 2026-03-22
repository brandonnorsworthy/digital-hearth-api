using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalHearth.Api.Controllers;

[ApiController]
public class TaskController(ICurrentUserService currentUser, ITaskService taskService) : ApiControllerBase
{
    [HttpGet("api/households/{householdId:int}/tasks")]
    public async Task<IActionResult> List(int householdId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await taskService.ListAsync(householdId, user!, ct));
    }

    [HttpPost("api/households/{householdId:int}/tasks")]
    public async Task<IActionResult> Create(int householdId, [FromBody] CreateTaskRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        var result = await taskService.CreateAsync(householdId, req, user!, ct);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(List), new { householdId }, result.Value);
    }

    [HttpPut("api/tasks/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await taskService.UpdateAsync(id, req, user!, ct));
    }

    [HttpDelete("api/tasks/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await taskService.DeleteAsync(id, user!, ct));
    }

    [HttpPost("api/tasks/{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await taskService.CompleteAsync(id, user!, ct));
    }

    [HttpGet("api/tasks/{id:int}/history")]
    public async Task<IActionResult> History(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await taskService.GetHistoryAsync(id, user!, ct));
    }
}
