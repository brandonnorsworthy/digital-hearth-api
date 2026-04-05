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

    public async Task<ServiceResult> ChangePinAsync(Guid userId, ChangePinRequest req, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.Unauthorized("Not authenticated");

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPin, user.PinHash))
            return ServiceResult.Unauthorized("Current PIN is incorrect");

        if (req.NewPin.Length != 4 || !req.NewPin.All(char.IsDigit))
            return ServiceResult.BadRequest("New PIN must be exactly 4 digits");

        var newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPin);
        await users.UpdatePinHashAsync(userId, newHash, ct);

        return ServiceResult.Ok();
    }
}
