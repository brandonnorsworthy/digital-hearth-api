using DigitalHearth.Api.Data;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<PushSubscription?> GetSubscriptionAsync(int userId, string endpoint, CancellationToken ct)
    {
        return await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint, ct);
    }

    public async Task<List<PushSubscription>> GetSubscriptionsByUserAsync(int userId, CancellationToken ct)
    {
        return await db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task AddSubscriptionAsync(PushSubscription sub, CancellationToken ct)
    {
        db.PushSubscriptions.Add(sub);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteSubscriptionAsync(PushSubscription sub, CancellationToken ct)
    {
        db.PushSubscriptions.Remove(sub);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteSubscriptionsAsync(List<PushSubscription> subs, CancellationToken ct)
    {
        db.PushSubscriptions.RemoveRange(subs);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<int>> GetOptedOutTaskIdsAsync(int userId, CancellationToken ct)
    {
        return await db.NotifPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.TaskId)
            .ToListAsync(ct);
    }

    public async Task<bool> IsOptedOutAsync(int userId, int taskId, CancellationToken ct)
    {
        return await db.NotifPreferences
            .AnyAsync(p => p.UserId == userId && p.TaskId == taskId, ct);
    }

    public async Task AddPreferenceAsync(NotifPreference pref, CancellationToken ct)
    {
        db.NotifPreferences.Add(pref);
        await db.SaveChangesAsync(ct);
    }

    public async Task<NotifPreference?> GetPreferenceAsync(int userId, int taskId, CancellationToken ct)
    {
        return await db.NotifPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TaskId == taskId, ct);
    }

    public async Task DeletePreferenceAsync(NotifPreference pref, CancellationToken ct)
    {
        db.NotifPreferences.Remove(pref);
        await db.SaveChangesAsync(ct);
    }
}
