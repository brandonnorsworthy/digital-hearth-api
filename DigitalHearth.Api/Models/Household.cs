namespace DigitalHearth.Api.Models;

public class Household
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string JoinCode { get; set; } = null!;
    public int WeekResetDay { get; set; } = 1; // 0=Sun … 6=Sat, default Monday

    public ICollection<User> Members { get; set; } = [];
    public ICollection<RecurringTask> Tasks { get; set; } = [];
    public ICollection<WeeklyMeal> WeeklyMeals { get; set; } = [];
    public ICollection<MealLibrary> MealLibrary { get; set; } = [];
}
