namespace DigitalHearth.Api.Models;

public class MealLibrary
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public string Name { get; set; } = null!;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string[] Tags { get; set; } = [];
    public Image? Image { get; set; }

    public Household Household { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<WeeklyMeal> WeeklyMeals { get; set; } = [];
}
