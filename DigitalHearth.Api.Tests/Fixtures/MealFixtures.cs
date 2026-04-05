using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Tests.Fixtures;

public static class MealFixtures
{
    private static readonly DateTime BaseTime = new(2025, 1, 6, 0, 0, 0, DateTimeKind.Utc); // Monday

    public static readonly Guid DefaultMealId = new("00000000-0000-0000-0000-000000000001");

    public static MealLibrary LibraryMeal(Guid? id = null, Guid? householdId = null, string name = "Pasta") => new()
    {
        Id = id ?? DefaultMealId,
        HouseholdId = householdId ?? UserFixtures.DefaultHouseholdId,
        Name = name,
        CreatedByUserId = UserFixtures.DefaultId,
        CreatedByUser = UserFixtures.Member(),
        CreatedAt = BaseTime,
        Tags = [],
        Image = null
    };

    public static WeeklyMeal Weekly(Guid? id = null, Guid? householdId = null, string name = "Pasta", Guid? libraryId = null) => new()
    {
        Id = id ?? DefaultMealId,
        HouseholdId = householdId ?? UserFixtures.DefaultHouseholdId,
        WeekOf = DateOnly.FromDateTime(BaseTime),
        Name = name,
        MealLibraryId = libraryId,
        MealLibrary = null
    };
}
