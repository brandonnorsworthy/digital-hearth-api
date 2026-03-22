namespace DigitalHearth.Api.DTOs.Meal;

public record WeeklyMealResponse(int Id, string WeekOf, string Name, int? MealLibraryId, bool IsFromLibrary, string? ImageUrl);
