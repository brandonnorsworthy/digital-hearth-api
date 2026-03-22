using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Controllers;

[Route("api/auth")]
public class AuthController(AppDbContext db, ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == req.Username.ToLower(), ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Pin, user.PinHash))
            return Unauthorized(new { error = "Invalid credentials" });

        currentUser.SetUserId(user.Id);

        return Ok(new MeResponse(user.Id, user.Username, user.HouseholdId));
    }

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
}
