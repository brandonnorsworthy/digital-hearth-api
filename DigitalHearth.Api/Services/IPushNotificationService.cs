namespace DigitalHearth.Api.Services;

public interface IPushNotificationService
{
    Task SendToUserAsync(Guid userId, string title, string body, CancellationToken ct = default);
    Task<bool> SendToSubscriptionAsync(Models.PushSubscription sub, string title, string body, CancellationToken ct = default);
}
