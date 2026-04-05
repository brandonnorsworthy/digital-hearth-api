namespace DigitalHearth.Api.DTOs.Meal;

public record AddWeeklyMealRequest(string WeekOf, Guid? MealLibraryId, string? Name);
