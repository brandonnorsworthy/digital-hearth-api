using DigitalHearth.Api.Models;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalHearth.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected async Task<(User? user, IActionResult? error)> RequireUserAsync(
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var user = await currentUser.GetUserAsync(ct);
        if (user is null)
            return (null, Unauthorized(new { error = "Not authenticated" }));
        return (user, null);
    }

    protected IActionResult ToActionResult(ServiceResult result) => result.Status switch
    {
        ServiceResultStatus.Ok => NoContent(),
        ServiceResultStatus.NotFound => NotFound(new { error = result.Error }),
        ServiceResultStatus.Forbidden => StatusCode(403, new { error = "Forbidden" }),
        ServiceResultStatus.BadRequest => BadRequest(new { error = result.Error }),
        ServiceResultStatus.Conflict => Conflict(new { error = result.Error }),
        ServiceResultStatus.Unauthorized => Unauthorized(new { error = result.Error }),
        _ => StatusCode(500)
    };

    protected IActionResult ToActionResult<T>(ServiceResult<T> result) => result.Status switch
    {
        ServiceResultStatus.Ok => Ok(result.Value),
        _ => ToActionResult((ServiceResult)result)
    };
}
