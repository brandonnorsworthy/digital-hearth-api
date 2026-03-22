namespace DigitalHearth.Api.Services;

public interface IPushNotificationService
{
    Task SendToUserAsync(int userId, string title, string body, CancellationToken ct = default);
}
