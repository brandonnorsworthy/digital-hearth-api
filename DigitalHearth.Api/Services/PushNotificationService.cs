using System.Text.Json;
using DigitalHearth.Api.Repositories;
using WebPush;

namespace DigitalHearth.Api.Services;

public class PushNotificationService(INotificationRepository notifications, IConfiguration config, ILogger<PushNotificationService> logger)
    : IPushNotificationService
{
    private VapidDetails? GetVapid()
    {
        var subject = config["Vapid:Subject"];
        var publicKey = config["Vapid:PublicKey"];
        var privateKey = config["Vapid:PrivateKey"];

        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            return null;

        return new VapidDetails(subject, publicKey, privateKey);
    }

    public async Task SendToUserAsync(Guid userId, string title, string body, CancellationToken ct = default)
    {
        var subs = await notifications.GetSubscriptionsByUserAsync(userId, ct);
        foreach (var sub in subs)
            await SendToSubscriptionAsync(sub, title, body, ct);
    }

    public async Task<bool> SendToSubscriptionAsync(Models.PushSubscription sub, string title, string body, CancellationToken ct = default)
    {
        var vapid = GetVapid();
        if (vapid is null)
        {
            logger.LogWarning("VAPID keys not configured — skipping push notification");
            return false;
        }

        try
        {
            var client = new WebPushClient();
            var payload = JsonSerializer.Serialize(new { title, body });
            var pushSub = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
            await client.SendNotificationAsync(pushSub, payload, vapid);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send push notification to subscription {Id} — may be stale", sub.Id);
            return false;
        }
    }
}
