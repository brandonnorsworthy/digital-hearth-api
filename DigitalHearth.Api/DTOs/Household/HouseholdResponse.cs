namespace DigitalHearth.Api.DTOs.Household;

public record HouseholdResponse(int Id, string Name, string JoinCode, string WeekResetDay, int? GoalMealsPerWeek, int? MonthlyImageBudget, int ImageGenThisMonth);
