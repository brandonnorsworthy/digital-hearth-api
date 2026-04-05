namespace DigitalHearth.Api.Models;

public class Household
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string JoinCode { get; set; } = null!;
    public int WeekResetDay { get; set; } = 1; // 0=Sun … 6=Sat, default Monday
    public int? GoalMealsPerWeek { get; set; }
    public int? MonthlyImageBudget { get; set; } = 0; // max AI image generations per month, null = unlimited, 0 = disabled
    public int ImageGenCount { get; set; } = 0;  // images generated in the current tracked month
    public int ImageGenMonth { get; set; } = 0;  // yyyyMM format, 0 = never tracked

    public ICollection<User> Members { get; set; } = [];
    public ICollection<RecurringTask> Tasks { get; set; } = [];
    public ICollection<WeeklyMeal> WeeklyMeals { get; set; } = [];
    public ICollection<MealLibrary> MealLibrary { get; set; } = [];
}
