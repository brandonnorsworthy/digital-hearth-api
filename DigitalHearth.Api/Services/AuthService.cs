using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.Repositories;

namespace DigitalHearth.Api.Services;

public class AuthService(IUserRepository users, ICurrentUserService currentUser) : IAuthService
{
    public async Task<ServiceResult<MeResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await users.GetByUsernameAsync(req.Username, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Pin, user.PinHash))
            return ServiceResult<MeResponse>.Unauthorized("Invalid credentials");

        currentUser.SetUserId(user.Id);

        return ServiceResult<MeResponse>.Ok(new MeResponse(user.Id, user.Username, user.HouseholdId));
    }
}
