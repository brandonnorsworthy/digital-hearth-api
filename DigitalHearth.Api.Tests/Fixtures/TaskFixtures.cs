using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Tests.Fixtures;

public static class TaskFixtures
{
    private static readonly DateTime BaseTime = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Guid DefaultId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultId2 = new("00000000-0000-0000-0000-000000000002");
    public static readonly Guid DefaultId3 = new("00000000-0000-0000-0000-000000000003");

    public static RecurringTask NeverCompleted(Guid? id = null, Guid? householdId = null, int intervalDays = 7) => new()
    {
        Id = id ?? DefaultId,
        HouseholdId = householdId ?? UserFixtures.DefaultHouseholdId,
        Name = "Take out trash",
        IntervalDays = intervalDays,
        CreatedAt = BaseTime,
        LastCompletedAt = null,
        LastCompletedByUserId = null,
        LastCompletedByUser = null
    };

    public static RecurringTask Completed(Guid? id = null, Guid? householdId = null, int intervalDays = 7, DateTime? completedAt = null) => new()
    {
        Id = id ?? DefaultId,
        HouseholdId = householdId ?? UserFixtures.DefaultHouseholdId,
        Name = "Take out trash",
        IntervalDays = intervalDays,
        CreatedAt = BaseTime,
        LastCompletedAt = completedAt ?? BaseTime.AddDays(3),
        LastCompletedByUserId = UserFixtures.DefaultId,
        LastCompletedByUser = UserFixtures.Member()
    };
}
