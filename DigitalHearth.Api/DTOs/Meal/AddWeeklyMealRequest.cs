namespace DigitalHearth.Api.DTOs.Meal;

public record AddWeeklyMealRequest(string WeekOf, int? MealLibraryId, string? Name);
