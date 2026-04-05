namespace DigitalHearth.Api.Models;

public class PushSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = null!;
    public string P256dh { get; set; } = null!;
    public string Auth { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSuccessfulPushAt { get; set; }

    public User User { get; set; } = null!;
}
