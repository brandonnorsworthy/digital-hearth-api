using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Controllers;

[Route("api/auth")]
public class AuthController(AppDbContext db, ICurrentUserService currentUser) : ApiControllerBase
{
    // Pre-computed hash used when the username isn't found, so response time
    // is the same regardless of whether the username exists (prevents enumeration).
    private const string DummyHash = "$2a$11$eSPCGzGJcO1ZFsJBKXSaXO2UTtb2qmI9ldxZR.4zUPQTbqnGxPVUe";

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var normalizedUsername = req.Username.ToLowerInvariant();
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername, ct);

        var hash = user?.PinHash ?? DummyHash;
        if (!BCrypt.Net.BCrypt.Verify(req.Pin, hash) || user is null)
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
