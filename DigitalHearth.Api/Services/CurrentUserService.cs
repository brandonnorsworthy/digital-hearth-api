using DigitalHearth.Api.Data;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    : ICurrentUserService
{
    private const string SessionKey = "userId";

    public int? GetUserId()
    {
        var session = httpContextAccessor.HttpContext?.Session;
        return session?.GetInt32(SessionKey);
    }

    public bool IsAuthenticated => GetUserId().HasValue;

    public async Task<User?> GetUserAsync(CancellationToken ct = default)
    {
        var id = GetUserId();
        if (id is null) return null;
        return await db.Users.FindAsync([id.Value], ct);
    }

    public void SetUserId(int userId)
        => httpContextAccessor.HttpContext!.Session.SetInt32(SessionKey, userId);

    public void Clear()
        => httpContextAccessor.HttpContext!.Session.Clear();
}
