using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Tests.Fixtures;

public static class HouseholdFixtures
{
    public static Household Default(Guid? id = null, string name = "Test House", string joinCode = "ABC123", int weekResetDay = 1) => new()
    {
        Id = id ?? UserFixtures.DefaultHouseholdId,
        Name = name,
        JoinCode = joinCode,
        WeekResetDay = weekResetDay,
        MonthlyImageBudget = null
    };
}
