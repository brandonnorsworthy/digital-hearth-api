using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Repositories;

public interface INotificationRepository
{
    Task<PushSubscription?> GetSubscriptionAsync(Guid userId, string endpoint, CancellationToken ct);
    Task<List<PushSubscription>> GetSubscriptionsByUserAsync(Guid userId, CancellationToken ct);
    Task AddSubscriptionAsync(PushSubscription sub, CancellationToken ct);
    Task DeleteSubscriptionAsync(PushSubscription sub, CancellationToken ct);
    Task DeleteSubscriptionsAsync(List<PushSubscription> subs, CancellationToken ct);
    Task<List<Guid>> GetOptedOutTaskIdsAsync(Guid userId, CancellationToken ct);
    Task<bool> IsOptedOutAsync(Guid userId, Guid taskId, CancellationToken ct);
    Task AddPreferenceAsync(NotifPreference pref, CancellationToken ct);
    Task<NotifPreference?> GetPreferenceAsync(Guid userId, Guid taskId, CancellationToken ct);
    Task DeletePreferenceAsync(NotifPreference pref, CancellationToken ct);
    Task<bool> HasLogAsync(Guid subscriptionId, Guid taskId, DateTime dueAt, CancellationToken ct);
    Task AddLogAsync(NotificationLog log, CancellationToken ct);
    Task<UserNotifSettings?> GetUserNotifSettingsAsync(Guid userId, CancellationToken ct);
    Task UpsertUserNotifSettingsAsync(UserNotifSettings settings, CancellationToken ct);
}
