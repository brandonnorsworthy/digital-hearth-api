namespace DigitalHearth.Api.DTOs.Household;

public record UpdateHouseholdRequest(string? Name, string? WeekResetDay, int? GoalMealsPerWeek);
