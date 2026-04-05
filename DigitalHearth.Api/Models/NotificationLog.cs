namespace DigitalHearth.Api.Models;

public enum NotificationStatus { Sent, Failed }

public class NotificationLog
{
    public Guid Id { get; set; }
    public Guid PushSubscriptionId { get; set; }
    public PushSubscription PushSubscription { get; set; } = null!;
    public Guid RecurringTaskId { get; set; }
    public RecurringTask RecurringTask { get; set; } = null!;
    public DateTime DueAt { get; set; }
    public DateTime SentAt { get; set; }
    public NotificationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
