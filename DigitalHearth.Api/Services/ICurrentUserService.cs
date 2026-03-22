using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface ICurrentUserService
{
    int? GetUserId();
    bool IsAuthenticated { get; }
    Task<User?> GetUserAsync(CancellationToken ct = default);
    void SetUserId(int userId);
    void Clear();
}
