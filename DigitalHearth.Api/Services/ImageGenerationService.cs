using OpenAI.Chat;
using OpenAI.Images;

namespace DigitalHearth.Api.Services;

public class ImageGenerationService(IConfiguration config, ILogger<ImageGenerationService> logger) : IImageGenerationService
{
    private const string PromptSystemMessage =
        "You are a professional food photographer and stylist. " +
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
            // Stage 1: generate a rich photography prompt via GPT
            var chatClient = new ChatClient("gpt-4o-mini", apiKey);
            var chatResult = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(PromptSystemMessage),
                new UserChatMessage(mealName)
            ], cancellationToken: ct);

            var imagePrompt = chatResult.Value.Content[0].Text;
            logger.LogInformation("Image prompt for '{MealName}': {Prompt}", mealName, imagePrompt);

            // Stage 2: pass the generated prompt to the image model
            var imageClient = new ImageClient("dall-e-3", apiKey);
            var imageResult = await imageClient.GenerateImageAsync(imagePrompt, new ImageGenerationOptions
            {
                Size = GeneratedImageSize.W1024xH1024
            }, ct);

            return imageResult.Value.ImageUri?.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image generation failed for meal '{MealName}'", mealName);
            return null;
        }
    }
}
