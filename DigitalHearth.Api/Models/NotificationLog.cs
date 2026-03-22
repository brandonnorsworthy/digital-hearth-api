namespace DigitalHearth.Api.Models;

public enum NotificationStatus { Sent, Failed }

public class NotificationLog
{
    public int Id { get; set; }
    public int PushSubscriptionId { get; set; }
    public PushSubscription PushSubscription { get; set; } = null!;
    public int RecurringTaskId { get; set; }
    public RecurringTask RecurringTask { get; set; } = null!;
    public DateTime DueAt { get; set; }
    public DateTime SentAt { get; set; }
    public NotificationStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
