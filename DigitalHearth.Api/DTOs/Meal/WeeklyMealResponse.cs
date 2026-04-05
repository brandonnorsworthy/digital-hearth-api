namespace DigitalHearth.Api.DTOs.Meal;

public record WeeklyMealResponse(Guid Id, string WeekOf, string Name, Guid? MealLibraryId, bool IsFromLibrary, bool HasImage, string? ImageGuid);
