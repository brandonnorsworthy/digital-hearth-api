namespace DigitalHearth.Api.Models;

public class UserNotifSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Hour of day (0–12) for daily task reminder. Null = not set.</summary>
    public int? TaskReminderHour { get; set; }

    /// <summary>How many days ahead to send medium-term task reminders (1–7). Null = not set.</summary>
    public int? MediumTermDaysAhead { get; set; }

    public bool MealPlannerNotifs { get; set; } = true;
    public bool ShortTermTaskNotifs { get; set; } = true;
    public bool MediumTermTaskNotifs { get; set; } = true;
    public bool LongTermTaskNotifs { get; set; } = true;
    public bool TaskCompletedNotifs { get; set; } = false;

    public User User { get; set; } = null!;
}
