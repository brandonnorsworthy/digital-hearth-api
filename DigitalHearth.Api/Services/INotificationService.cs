using DigitalHearth.Api.DTOs.Notification;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface INotificationService
{
    Task<ServiceResult> SubscribeAsync(PushSubscriptionRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult> UnsubscribeAsync(User user, CancellationToken ct = default);
    Task<ServiceResult<PreferencesResponse>> GetPreferencesAsync(int householdId, User user, CancellationToken ct = default);
    Task<ServiceResult> OptOutAsync(OptOutRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult> RemoveOptOutAsync(int taskId, User user, CancellationToken ct = default);
}
