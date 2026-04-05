namespace DigitalHearth.Api.Models;

// Only opt-OUT rows are stored. Absence = opted in.
public class NotifPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TaskId { get; set; }

    public User User { get; set; } = null!;
    public RecurringTask Task { get; set; } = null!;
}
