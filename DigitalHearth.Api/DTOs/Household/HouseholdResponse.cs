namespace DigitalHearth.Api.DTOs.Household;

public record HouseholdResponse(Guid Id, string Name, string JoinCode, DateTime JoinCodeExpiresAt, string WeekResetDay, int? GoalMealsPerWeek, int? MonthlyImageBudget, int ImageGenThisMonth);
