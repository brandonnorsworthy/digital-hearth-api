namespace DigitalHearth.Api.Models;

public class RecurringTask
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public string Name { get; set; } = null!;
    public int IntervalDays { get; set; }
    public bool IsOneTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public Guid? LastCompletedByUserId { get; set; }

    public Household Household { get; set; } = null!;
    public User? LastCompletedByUser { get; set; }
    public ICollection<TaskCompletion> Completions { get; set; } = [];
    public ICollection<NotifPreference> NotifPreferences { get; set; } = [];
}
