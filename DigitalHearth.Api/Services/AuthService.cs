using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.Repositories;

namespace DigitalHearth.Api.Services;

public class AuthService(IUserRepository users, ICurrentUserService currentUser) : IAuthService
{
    public async Task<ServiceResult<MeResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await users.GetByUsernameAsync(req.Username, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return ServiceResult<MeResponse>.Unauthorized("Invalid credentials");

        if (!user.IsActive)
            return ServiceResult<MeResponse>.Unauthorized("Invalid credentials");

        currentUser.SetUserId(user.Id);

        return ServiceResult<MeResponse>.Ok(new MeResponse(user.Id, user.Username, user.HouseholdId));
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.Unauthorized("Not authenticated");

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return ServiceResult.Unauthorized("Current password is incorrect");

        if (!IsValidPassword(req.NewPassword))
            return ServiceResult.BadRequest("Password must be at least 10 characters and include uppercase, lowercase, a number, and a special character");

        var newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await users.UpdatePasswordHashAsync(userId, newHash, ct);

        return ServiceResult.Ok();
    }

    private static bool IsValidPassword(string password) =>
        password.Length >= 10 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(c => !char.IsLetterOrDigit(c));
}
