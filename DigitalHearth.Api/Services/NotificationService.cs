using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Notification;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Services;

public class NotificationService(AppDbContext db) : INotificationService
{
    public async Task<ServiceResult> SubscribeAsync(PushSubscriptionRequest req, User user, CancellationToken ct = default)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Endpoint == req.Endpoint, ct);

        if (existing is not null)
            db.PushSubscriptions.Remove(existing);

        db.PushSubscriptions.Add(new PushSubscription
        {
            UserId = user.Id,
            Endpoint = req.Endpoint,
            P256dh = req.P256dh,
            Auth = req.Auth
        });

        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnsubscribeAsync(User user, CancellationToken ct = default)
    {
        var subs = await db.PushSubscriptions
            .Where(s => s.UserId == user.Id)
            .ToListAsync(ct);

        db.PushSubscriptions.RemoveRange(subs);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<PreferencesResponse>> GetPreferencesAsync(
        int householdId, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<PreferencesResponse>.Forbidden();

        var optedOutIds = await db.NotifPreferences
            .Where(p => p.UserId == user.Id)
            .Select(p => p.TaskId)
            .ToListAsync(ct);

        return ServiceResult<PreferencesResponse>.Ok(new PreferencesResponse(optedOutIds));
    }

    public async Task<ServiceResult> OptOutAsync(OptOutRequest req, User user, CancellationToken ct = default)
    {
        var alreadyOptedOut = await db.NotifPreferences
            .AnyAsync(p => p.UserId == user.Id && p.TaskId == req.TaskId, ct);

        if (!alreadyOptedOut)
        {
            db.NotifPreferences.Add(new NotifPreference
            {
                UserId = user.Id,
                TaskId = req.TaskId
            });
            await db.SaveChangesAsync(ct);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveOptOutAsync(int taskId, User user, CancellationToken ct = default)
    {
        var pref = await db.NotifPreferences
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.TaskId == taskId, ct);

        if (pref is not null)
        {
            db.NotifPreferences.Remove(pref);
            await db.SaveChangesAsync(ct);
        }

        return ServiceResult.Ok();
    }
}
