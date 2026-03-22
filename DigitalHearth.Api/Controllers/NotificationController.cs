using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Notification;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Controllers;

[ApiController]
public class NotificationController(AppDbContext db, ICurrentUserService currentUser, IConfiguration config) : ApiControllerBase
{
    private readonly IConfiguration _config = config;
    [HttpGet("api/notifications/vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = _config["Vapid:PublicKey"];
        if (string.IsNullOrEmpty(publicKey))
            return StatusCode(503, new { error = "VAPID keys not configured" });

        return Ok(new { publicKey });
    }

    [HttpPost("api/notifications/subscription")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        // Remove existing subscription with the same endpoint for this user (upsert)
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == user!.Id && s.Endpoint == req.Endpoint, ct);

        if (existing is not null)
            db.PushSubscriptions.Remove(existing);

        db.PushSubscriptions.Add(new PushSubscription
        {
            UserId = user!.Id,
            Endpoint = req.Endpoint,
            P256dh = req.P256dh,
            Auth = req.Auth
        });

        await db.SaveChangesAsync(ct);
        return StatusCode(201);
    }

    [HttpDelete("api/notifications/subscription")]
    public async Task<IActionResult> Unsubscribe(CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var subs = await db.PushSubscriptions
            .Where(s => s.UserId == user!.Id)
            .ToListAsync(ct);

        db.PushSubscriptions.RemoveRange(subs);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("api/households/{householdId:int}/notifications/preferences")]
    public async Task<IActionResult> GetPreferences(int householdId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != householdId) return Forbid();

        var optedOutIds = await db.NotifPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.TaskId)
            .ToListAsync(ct);

        return Ok(new PreferencesResponse(optedOutIds));
    }

    [HttpPost("api/notifications/preferences/opt-out")]
    public async Task<IActionResult> OptOut([FromBody] OptOutRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var alreadyOptedOut = await db.NotifPreferences
            .AnyAsync(p => p.UserId == user!.Id && p.TaskId == req.TaskId, ct);

        if (!alreadyOptedOut)
        {
            db.NotifPreferences.Add(new NotifPreference
            {
                UserId = user!.Id,
                TaskId = req.TaskId
            });
            await db.SaveChangesAsync(ct);
        }

        return StatusCode(201);
    }

    [HttpDelete("api/notifications/preferences/opt-out/{taskId:int}")]
    public async Task<IActionResult> RemoveOptOut(int taskId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var pref = await db.NotifPreferences
            .FirstOrDefaultAsync(p => p.UserId == user!.Id && p.TaskId == taskId, ct);

        if (pref is not null)
        {
            db.NotifPreferences.Remove(pref);
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }
}
