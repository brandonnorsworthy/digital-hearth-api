using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Tests.Fixtures;

public static class TaskFixtures
{
    private static readonly DateTime BaseTime = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static RecurringTask NeverCompleted(int id = 1, int householdId = 10, int intervalDays = 7) => new()
    {
        Id = id,
        HouseholdId = householdId,
        Name = "Take out trash",
        IntervalDays = intervalDays,
        CreatedAt = BaseTime,
        LastCompletedAt = null,
        LastCompletedByUserId = null,
        LastCompletedByUser = null
    };

    public static RecurringTask Completed(int id = 1, int householdId = 10, int intervalDays = 7, DateTime? completedAt = null) => new()
    {
        Id = id,
        HouseholdId = householdId,
        Name = "Take out trash",
        IntervalDays = intervalDays,
        CreatedAt = BaseTime,
        LastCompletedAt = completedAt ?? BaseTime.AddDays(3),
        LastCompletedByUserId = 1,
        LastCompletedByUser = UserFixtures.Member()
    };
}
