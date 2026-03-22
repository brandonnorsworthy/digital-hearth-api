using DigitalHearth.Api.DTOs.Auth;

namespace DigitalHearth.Api.Services;

public interface IAuthService
{
    Task<ServiceResult<MeResponse>> LoginAsync(LoginRequest req, CancellationToken ct = default);
}
