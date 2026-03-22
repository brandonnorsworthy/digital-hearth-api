using DigitalHearth.Api.Data;
using DigitalHearth.Api.Models;
using Microsoft.AspNetCore.DataProtection;

namespace DigitalHearth.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db, IDataProtectionProvider dpProvider)
    : ICurrentUserService
{
    private const string CookieName = "dh_auth";
    private readonly IDataProtector _protector = dpProvider.CreateProtector("DigitalHearth.Auth");

    public int? GetUserId()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return null;
        if (!ctx.Request.Cookies.TryGetValue(CookieName, out var value)) return null;
        try
        {
            return int.Parse(_protector.Unprotect(value));
        }
        catch
        {
            return null;
        }
    }

    public bool IsAuthenticated => GetUserId().HasValue;

    public async Task<User?> GetUserAsync(CancellationToken ct = default)
    {
        var id = GetUserId();
        if (id is null) return null;
        return await db.Users.FindAsync([id.Value], ct);
    }

    public void SetUserId(int userId)
    {
        var value = _protector.Protect(userId.ToString());
        httpContextAccessor.HttpContext!.Response.Cookies.Append(CookieName, value, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.MaxValue,
        });
    }

    public void Clear()
        => httpContextAccessor.HttpContext!.Response.Cookies.Delete(CookieName);
}
