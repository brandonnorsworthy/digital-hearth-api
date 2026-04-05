using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Controllers;

[ApiController]
public class MealController(ICurrentUserService currentUser, IMealService mealService, IImageGenerationService imageGeneration, AppDbContext db) : ApiControllerBase
{
    [HttpPost("api/meals/generate-image")]
    public async Task<IActionResult> GenerateImage([FromBody] GenerateImageRequest req, CancellationToken ct)
    {
        var (_, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var imageData = await imageGeneration.GenerateImageAsync(req.MealName, ct);
        if (imageData is null)
            return BadRequest(new { error = "Image generation failed or is not configured" });

        return Ok(new { imageData });
    }

    [HttpGet("api/meals/library/{id:int}/image/{guid:guid}")]
    public async Task<IActionResult> GetLibraryMealImage(int id, Guid guid, CancellationToken ct)
    {
        var imageData = await db.Images
            .Where(i => i.MealLibraryId == id && i.ImageGuid == guid)
            .Select(i => i.ImageData)
            .FirstOrDefaultAsync(ct);

        if (imageData is null) return NotFound();

        // Strip the data URI prefix ("data:image/png;base64,") and decode
        var base64 = imageData.Contains(',') ? imageData[(imageData.IndexOf(',') + 1)..] : imageData;
        var bytes = Convert.FromBase64String(base64);

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(bytes, "image/png");
    }

    [HttpGet("api/households/{householdId:int}/meals/weekly")]
    public async Task<IActionResult> GetWeekly(int householdId, [FromQuery] string? weekOf, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await mealService.GetWeeklyAsync(householdId, weekOf, user!, ct));
    }

    [HttpPost("api/households/{householdId:int}/meals/weekly")]
    public async Task<IActionResult> AddWeekly(int householdId, [FromBody] AddWeeklyMealRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        var result = await mealService.AddWeeklyAsync(householdId, req, user!, ct);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetWeekly), new { householdId }, result.Value);
    }

    [HttpPatch("api/meals/weekly/{id:int}")]
    public async Task<IActionResult> PatchWeekly(int id, [FromBody] PatchWeeklyMealRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await mealService.LinkToLibraryAsync(id, req, user!, ct));
    }

    [HttpDelete("api/meals/weekly/{id:int}")]
    public async Task<IActionResult> DeleteWeekly(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await mealService.DeleteWeeklyAsync(id, user!, ct));
    }

    [HttpGet("api/households/{householdId:int}/meals/library")]
    public async Task<IActionResult> GetLibrary(int householdId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await mealService.GetLibraryAsync(householdId, user!, ct));
    }

    [HttpPost("api/households/{householdId:int}/meals/library")]
    public async Task<IActionResult> AddToLibrary(int householdId, [FromBody] AddLibraryMealRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        var result = await mealService.AddToLibraryAsync(householdId, req, user!, ct);
        if (!result.IsSuccess) return ToActionResult(result);
        return CreatedAtAction(nameof(GetLibrary), new { householdId }, result.Value);
    }

    [HttpDelete("api/meals/library/{id:int}")]
    public async Task<IActionResult> DeleteFromLibrary(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await mealService.DeleteFromLibraryAsync(id, user!, ct));
    }

    [HttpPost("api/meals/library/{id:int}/favorite")]
    public async Task<IActionResult> FavoriteMeal(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await mealService.ToggleFavoriteAsync(id, true, user!, ct));
    }

    [HttpDelete("api/meals/library/{id:int}/favorite")]
    public async Task<IActionResult> UnfavoriteMeal(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await mealService.ToggleFavoriteAsync(id, false, user!, ct));
    }

    [HttpPost("api/meals/library/{id:int}/regenerate-image")]
    public async Task<IActionResult> RegenerateImage(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;
        return ToActionResult(await mealService.RegenerateImageAsync(id, user!, ct));
    }
}
