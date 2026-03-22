namespace DigitalHearth.Api.Models;

public class RecurringTask
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string Name { get; set; } = null!;
    public int IntervalDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public int? LastCompletedByUserId { get; set; }

    public Household Household { get; set; } = null!;
    public User? LastCompletedByUser { get; set; }
    public ICollection<TaskCompletion> Completions { get; set; } = [];
    public ICollection<NotifPreference> NotifPreferences { get; set; } = [];
}
