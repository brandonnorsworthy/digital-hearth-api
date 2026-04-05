using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;
using Microsoft.AspNetCore.DataProtection;

namespace DigitalHearth.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserRepository users, IDataProtectionProvider dpProvider)
    : ICurrentUserService
{
    private const string CookieName = "dh_auth";
    private readonly IDataProtector _protector = dpProvider.CreateProtector("DigitalHearth.Auth");

    public Guid? GetUserId()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null) return null;
        if (!ctx.Request.Cookies.TryGetValue(CookieName, out var value)) return null;
        try
        {
            return Guid.Parse(_protector.Unprotect(value));
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
        return await users.GetByIdAsync(id.Value, ct);
    }

    public void SetUserId(Guid userId)
    {
        var value = _protector.Protect(userId.ToString());
        var ctx = httpContextAccessor.HttpContext!;
        ctx.Response.Cookies.Append(CookieName, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.MaxValue,
        });
    }

    public void Clear()
        => httpContextAccessor.HttpContext!.Response.Cookies.Delete(CookieName);
}
