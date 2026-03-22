using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Auth;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Services;

public class AuthService(AppDbContext db, ICurrentUserService currentUser) : IAuthService
{
    public async Task<ServiceResult<MeResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == req.Username.ToLower(), ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Pin, user.PinHash))
            return ServiceResult<MeResponse>.Unauthorized("Invalid credentials");

        currentUser.SetUserId(user.Id);

        return ServiceResult<MeResponse>.Ok(new MeResponse(user.Id, user.Username, user.HouseholdId));
    }
}
