namespace DigitalHearth.Api.DTOs.Notification;

public record PushSubscriptionRequest(string Endpoint, string P256dh, string Auth);
