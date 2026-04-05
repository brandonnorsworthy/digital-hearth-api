using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Tests.Fixtures;

public static class MealFixtures
{
    private static readonly DateTime BaseTime = new(2025, 1, 6, 0, 0, 0, DateTimeKind.Utc); // Monday

    public static MealLibrary LibraryMeal(int id = 1, int householdId = 10, string name = "Pasta") => new()
    {
        Id = id,
        HouseholdId = householdId,
        Name = name,
        CreatedByUserId = 1,
        CreatedByUser = UserFixtures.Member(),
        CreatedAt = BaseTime,
        Tags = [],
        Image = null
    };

    public static WeeklyMeal Weekly(int id = 1, int householdId = 10, string name = "Pasta", int? libraryId = null) => new()
    {
        Id = id,
        HouseholdId = householdId,
        WeekOf = DateOnly.FromDateTime(BaseTime),
        Name = name,
        MealLibraryId = libraryId,
        MealLibrary = null
    };
}
