namespace DigitalHearth.Api.Services;

public interface IImageGenerationService
{
    Task<string?> GenerateImageAsync(string mealName, CancellationToken ct = default);
}
