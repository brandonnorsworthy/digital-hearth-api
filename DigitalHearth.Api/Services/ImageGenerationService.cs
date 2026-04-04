using OpenAI.Chat;
using OpenAI.Images;

namespace DigitalHearth.Api.Services;

public class ImageGenerationService(IConfiguration config, ILogger<ImageGenerationService> logger) : IImageGenerationService
{
    private const string PromptSystemMessage = "You are a professional food photographer and stylist. " +
        "Given a dish name, write a single detailed image generation prompt for a photorealistic food photograph. " +
        "Describe the plating, garnishes, textures, lighting, background, and atmosphere in vivid detail. " +
        "There should only be one dish in the image which is the main dish subject given to you. " +
        "Write only the prompt itself — no preamble, no explanation, no quotes.";

    public async Task<string?> GenerateImageAsync(string mealName, CancellationToken ct = default)
    {
        var apiKey = config["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("OpenAI:ApiKey is not configured — skipping image generation");
            return null;
        }

        try
        {
            var bytes = await GenerateImageBytesAsync(apiKey, mealName, ct);
            if (bytes is null) return null;
            return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image generation failed for meal '{MealName}'", mealName);
            return null;
        }
    }

    private async Task<byte[]?> GenerateImageBytesAsync(string apiKey, string mealName, CancellationToken ct)
    {
        // Stage 1: generate a rich photography prompt via GPT
        string imagePrompt;
        try
        {
            var chatClient = new ChatClient("gpt-4o-mini", apiKey);
            var chatResult = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(PromptSystemMessage),
                new UserChatMessage(mealName)
            ], cancellationToken: ct);

            imagePrompt = chatResult.Value.Content[0].Text;
            logger.LogInformation("Image prompt for '{MealName}': {Prompt}", mealName, imagePrompt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stage 1 (prompt generation) failed for meal '{MealName}'", mealName);
            throw;
        }

        // Stage 2: generate the image — gpt-image-1 returns bytes directly, no download needed
        try
        {
            var imageClient = new ImageClient("gpt-image-1", apiKey);
            var imageResult = await imageClient.GenerateImageAsync(imagePrompt, new ImageGenerationOptions
            {
                Size = GeneratedImageSize.W1024xH1024
            }, ct);

            var bytes = imageResult.Value.ImageBytes?.ToArray();
            logger.LogInformation("Image generated for '{MealName}' ({Bytes} bytes)", mealName, bytes?.Length ?? 0);
            return bytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stage 2 (image generation) failed for meal '{MealName}'", mealName);
            throw;
        }
    }
}
