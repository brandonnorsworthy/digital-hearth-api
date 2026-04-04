using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalHearth.Api.Controllers;

[Route("api/auth")]
public class AuthController(ICurrentUserService currentUser, IAuthService authService) : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
        => ToActionResult(await authService.LoginAsync(req, ct));

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        currentUser.Clear();
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        return Ok(new MeResponse(user!.Id, user.Username, user.HouseholdId));
    }

    [HttpPost("change-pin")]
    public async Task<IActionResult> ChangePin([FromBody] ChangePinRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        return ToActionResult(await authService.ChangePinAsync(user!.Id, req, ct));
    }
}
