using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Auth;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Services;

public class AuthService(AppDbContext db, ICurrentUserService currentUser) : IAuthService
{
    // Pre-computed hash used when the username isn't found, so response time
    // is the same regardless of whether the username exists (prevents enumeration).
    private const string DummyHash = "$2a$11$eSPCGzGJcO1ZFsJBKXSaXO2UTtb2qmI9ldxZR.4zUPQTbqnGxPVUe";

    public async Task<ServiceResult<MeResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var normalizedUsername = req.Username.ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername, ct);

        var hash = user?.PinHash ?? DummyHash;
        if (!BCrypt.Net.BCrypt.Verify(req.Pin, hash) || user is null)
            return ServiceResult<MeResponse>.Unauthorized("Invalid credentials");

        currentUser.SetUserId(user.Id);

        return ServiceResult<MeResponse>.Ok(new MeResponse(user.Id, user.Username, user.HouseholdId));
    }
}
