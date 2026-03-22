namespace DigitalHearth.Api.Models;

public class WeeklyMeal
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public DateOnly WeekOf { get; set; }
    public string Name { get; set; } = null!;
    public int? MealLibraryId { get; set; }

    public Household Household { get; set; } = null!;
    public MealLibrary? MealLibrary { get; set; }
}
