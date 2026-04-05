namespace DigitalHearth.Api.Models;

public class TaskCompletion
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CompletedAt { get; set; }

    public RecurringTask Task { get; set; } = null!;
    public User User { get; set; } = null!;
}
