namespace DigitalHearth.Api.DTOs.Meal;

public record LibraryMealResponse(int Id, string Name, string CreatedBy, DateTime CreatedAt, string[] Tags, bool HasImage, bool IsFavorited, string? ImageGuid);
