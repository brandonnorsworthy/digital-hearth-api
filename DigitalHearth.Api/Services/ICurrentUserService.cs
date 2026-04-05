using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface ICurrentUserService
{
    Guid? GetUserId();
    bool IsAuthenticated { get; }
    Task<User?> GetUserAsync(CancellationToken ct = default);
    void SetUserId(Guid userId);
    void Clear();
}
