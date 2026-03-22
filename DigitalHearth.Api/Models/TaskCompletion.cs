namespace DigitalHearth.Api.Models;

public class TaskCompletion
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public DateTime CompletedAt { get; set; }

    public RecurringTask Task { get; set; } = null!;
    public User User { get; set; } = null!;
}
