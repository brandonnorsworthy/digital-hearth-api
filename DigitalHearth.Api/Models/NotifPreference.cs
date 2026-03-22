namespace DigitalHearth.Api.Models;

// Only opt-OUT rows are stored. Absence = opted in.
public class NotifPreference
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TaskId { get; set; }

    public User User { get; set; } = null!;
    public RecurringTask Task { get; set; } = null!;
}
