namespace DigitalHearth.Api.Models;

public class WeeklyMeal
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public DateOnly WeekOf { get; set; }
    public string Name { get; set; } = null!;
    public Guid? MealLibraryId { get; set; }
    public bool IsCooked { get; set; }

    public Household Household { get; set; } = null!;
    public MealLibrary? MealLibrary { get; set; }
}
