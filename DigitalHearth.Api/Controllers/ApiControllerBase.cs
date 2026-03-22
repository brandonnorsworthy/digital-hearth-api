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
}
