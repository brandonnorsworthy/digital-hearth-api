namespace DigitalHearth.Api.Services;

public interface IImageGenerationService
{
    /// <summary>Returns a base64 data URI. Safe to persist.</summary>
    Task<string?> GenerateImageAsync(string mealName, CancellationToken ct = default);
}
