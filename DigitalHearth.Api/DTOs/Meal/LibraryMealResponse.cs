namespace DigitalHearth.Api.DTOs.Meal;

public record LibraryMealResponse(Guid Id, string Name, string CreatedBy, DateTime CreatedAt, string[] Tags, bool HasImage, bool IsFavorited, string? ImageGuid);
