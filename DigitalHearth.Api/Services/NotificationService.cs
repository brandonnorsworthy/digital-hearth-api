using DigitalHearth.Api.DTOs.Notification;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;

namespace DigitalHearth.Api.Services;

public class NotificationService(INotificationRepository notifications) : INotificationService
{
    public async Task<ServiceResult> SubscribeAsync(PushSubscriptionRequest req, User user, CancellationToken ct = default)
    {
        var existing = await notifications.GetSubscriptionAsync(user.Id, req.Endpoint, ct);

        if (existing is not null)
            await notifications.DeleteSubscriptionAsync(existing, ct);

        await notifications.AddSubscriptionAsync(new PushSubscription
        {
            UserId = user.Id,
            Endpoint = req.Endpoint,
            P256dh = req.P256dh,
            Auth = req.Auth
        }, ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnsubscribeAsync(User user, CancellationToken ct = default)
    {
        var subs = await notifications.GetSubscriptionsByUserAsync(user.Id, ct);

        await notifications.DeleteSubscriptionsAsync(subs, ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<PreferencesResponse>> GetPreferencesAsync(
        int householdId, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<PreferencesResponse>.Forbidden();

        var optedOutIds = await notifications.GetOptedOutTaskIdsAsync(user.Id, ct);

        return ServiceResult<PreferencesResponse>.Ok(new PreferencesResponse(optedOutIds));
    }

    public async Task<ServiceResult> OptOutAsync(OptOutRequest req, User user, CancellationToken ct = default)
    {
        var alreadyOptedOut = await notifications.IsOptedOutAsync(user.Id, req.TaskId, ct);

        if (!alreadyOptedOut)
        {
            await notifications.AddPreferenceAsync(new NotifPreference
            {
                UserId = user.Id,
                TaskId = req.TaskId
            }, ct);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveOptOutAsync(int taskId, User user, CancellationToken ct = default)
    {
        var pref = await notifications.GetPreferenceAsync(user.Id, taskId, ct);

        if (pref is not null)
        {
            await notifications.DeletePreferenceAsync(pref, ct);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<UserNotifSettingsResponse>> GetUserNotifSettingsAsync(
        User user, CancellationToken ct = default)
    {
        var settings = await notifications.GetUserNotifSettingsAsync(user.Id, ct);

        var response = settings is null
            ? new UserNotifSettingsResponse(null, null, true, true, true, true, false)
            : new UserNotifSettingsResponse(
                settings.TaskReminderHour,
                settings.MediumTermDaysAhead,
                settings.MealPlannerNotifs,
                settings.ShortTermTaskNotifs,
                settings.MediumTermTaskNotifs,
                settings.LongTermTaskNotifs,
                settings.TaskCompletedNotifs);

        return ServiceResult<UserNotifSettingsResponse>.Ok(response);
    }

    public async Task<ServiceResult> UpdateUserNotifSettingsAsync(
        UpdateUserNotifSettingsRequest req, User user, CancellationToken ct = default)
    {
        await notifications.UpsertUserNotifSettingsAsync(new UserNotifSettings
        {
            UserId = user.Id,
            TaskReminderHour = req.TaskReminderHour,
            MediumTermDaysAhead = req.MediumTermDaysAhead,
            MealPlannerNotifs = req.MealPlannerNotifs,
            ShortTermTaskNotifs = req.ShortTermTaskNotifs,
            MediumTermTaskNotifs = req.MediumTermTaskNotifs,
            LongTermTaskNotifs = req.LongTermTaskNotifs,
            TaskCompletedNotifs = req.TaskCompletedNotifs,
        }, ct);

        return ServiceResult.Ok();
    }
}
