using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalHearth.Api.Controllers;

[ApiController]
public class MealController(ICurrentUserService currentUser, IMealService mealService) : ApiControllerBase
{
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
}
