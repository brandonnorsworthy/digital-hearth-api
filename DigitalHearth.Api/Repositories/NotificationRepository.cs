using DigitalHearth.Api.Data;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<PushSubscription?> GetSubscriptionAsync(Guid userId, string endpoint, CancellationToken ct)
    {
        return await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint, ct);
    }

    public async Task<List<PushSubscription>> GetSubscriptionsByUserAsync(Guid userId, CancellationToken ct)
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
        await db.PushSubscriptions
            .Where(s => s.Id == sub.Id)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteSubscriptionsAsync(List<PushSubscription> subs, CancellationToken ct)
    {
        var ids = subs.Select(s => s.Id).ToList();
        await db.PushSubscriptions
            .Where(s => ids.Contains(s.Id))
            .ExecuteDeleteAsync(ct);
    }

    public async Task UpdateLastSuccessfulPushAsync(Guid subscriptionId, DateTime sentAt, CancellationToken ct)
    {
        await db.PushSubscriptions
            .Where(s => s.Id == subscriptionId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastSuccessfulPushAt, sentAt), ct);
    }

    public async Task DeleteStaleSubscriptionsAsync(DateTime cutoff, CancellationToken ct)
    {
        await db.PushSubscriptions
            .Where(s => (s.LastSuccessfulPushAt == null && s.CreatedAt < cutoff)
                     || (s.LastSuccessfulPushAt != null && s.LastSuccessfulPushAt < cutoff))
            .ExecuteDeleteAsync(ct);
    }

    public async Task<List<Guid>> GetOptedOutTaskIdsAsync(Guid userId, CancellationToken ct)
    {
        return await db.NotifPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.TaskId)
            .ToListAsync(ct);
    }

    public async Task<bool> IsOptedOutAsync(Guid userId, Guid taskId, CancellationToken ct)
    {
        return await db.NotifPreferences
            .AnyAsync(p => p.UserId == userId && p.TaskId == taskId, ct);
    }

    public async Task AddPreferenceAsync(NotifPreference pref, CancellationToken ct)
    {
        db.NotifPreferences.Add(pref);
        await db.SaveChangesAsync(ct);
    }

    public async Task<NotifPreference?> GetPreferenceAsync(Guid userId, Guid taskId, CancellationToken ct)
    {
        return await db.NotifPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.TaskId == taskId, ct);
    }

    public async Task DeletePreferenceAsync(NotifPreference pref, CancellationToken ct)
    {
        db.NotifPreferences.Remove(pref);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> HasLogAsync(Guid subscriptionId, Guid taskId, DateTime dueAt, CancellationToken ct)
    {
        return await db.NotificationLogs
            .AnyAsync(l => l.PushSubscriptionId == subscriptionId
                        && l.RecurringTaskId == taskId
                        && l.DueAt == dueAt, ct);
    }

    public async Task AddLogAsync(NotificationLog log, CancellationToken ct)
    {
        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserNotifSettings?> GetUserNotifSettingsAsync(Guid userId, CancellationToken ct)
    {
        return await db.UserNotifSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);
    }

    public async Task UpsertUserNotifSettingsAsync(UserNotifSettings settings, CancellationToken ct)
    {
        var existing = await db.UserNotifSettings
            .FirstOrDefaultAsync(s => s.UserId == settings.UserId, ct);

        if (existing is null)
        {
            db.UserNotifSettings.Add(settings);
        }
        else
        {
            existing.TaskReminderHour = settings.TaskReminderHour;
            existing.MediumTermDaysAhead = settings.MediumTermDaysAhead;
            existing.MealPlannerNotifs = settings.MealPlannerNotifs;
            existing.ShortTermTaskNotifs = settings.ShortTermTaskNotifs;
            existing.MediumTermTaskNotifs = settings.MediumTermTaskNotifs;
            existing.LongTermTaskNotifs = settings.LongTermTaskNotifs;
            existing.TaskCompletedNotifs = settings.TaskCompletedNotifs;
        }

        await db.SaveChangesAsync(ct);
    }
}
