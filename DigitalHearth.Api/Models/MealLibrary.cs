namespace DigitalHearth.Api.Models;

public class MealLibrary
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public string Name { get; set; } = null!;
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string[] Tags { get; set; } = [];
    public string? ImageUrl { get; set; }

    public Household Household { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<WeeklyMeal> WeeklyMeals { get; set; } = [];
}
