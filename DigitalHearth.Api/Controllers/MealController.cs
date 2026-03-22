using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Controllers;

[ApiController]
public class MealController(AppDbContext db, ICurrentUserService currentUser) : ApiControllerBase
{
    private static WeeklyMealResponse ToWeeklyResponse(WeeklyMeal m) =>
        new(m.Id, m.WeekOf.ToString("yyyy-MM-dd"), m.Name, m.MealLibraryId, m.MealLibraryId.HasValue);

    private static LibraryMealResponse ToLibraryResponse(MealLibrary m) =>
        new(m.Id, m.Name, m.CreatedByUser.Username, m.CreatedAt);

    private static DateOnly CurrentWeekMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var offset = ((int)today.DayOfWeek + 6) % 7; // Mon=0
        return today.AddDays(-offset);
    }

    [HttpGet("api/households/{householdId:int}/meals/weekly")]
    public async Task<IActionResult> GetWeekly(int householdId, [FromQuery] string? weekOf, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != householdId) return Forbid();

        var week = weekOf is not null && DateOnly.TryParse(weekOf, out var parsed)
            ? parsed
            : CurrentWeekMonday();

        var meals = await db.WeeklyMeals
            .Where(m => m.HouseholdId == householdId && m.WeekOf == week)
            .ToListAsync(ct);

        return Ok(meals.Select(ToWeeklyResponse));
    }

    [HttpPost("api/households/{householdId:int}/meals/weekly")]
    public async Task<IActionResult> AddWeekly(int householdId, [FromBody] AddWeeklyMealRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != householdId) return Forbid();

        if (!DateOnly.TryParse(req.WeekOf, out var weekOf))
            return BadRequest(new { error = "weekOf must be a valid date (YYYY-MM-DD)" });

        string name;
        int? libraryId = null;

        if (req.MealLibraryId.HasValue)
        {
            var libEntry = await db.MealLibrary.FindAsync([req.MealLibraryId.Value], ct);
            if (libEntry is null || libEntry.HouseholdId != householdId)
                return NotFound(new { error = "Library meal not found" });
            name = libEntry.Name;
            libraryId = libEntry.Id;
        }
        else if (!string.IsNullOrWhiteSpace(req.Name))
        {
            name = req.Name;
        }
        else
        {
            return BadRequest(new { error = "Either mealLibraryId or name is required" });
        }

        var meal = new WeeklyMeal
        {
            HouseholdId = householdId,
            WeekOf = weekOf,
            Name = name,
            MealLibraryId = libraryId
        };

        db.WeeklyMeals.Add(meal);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetWeekly), new { householdId }, ToWeeklyResponse(meal));
    }

    [HttpDelete("api/meals/weekly/{id:int}")]
    public async Task<IActionResult> DeleteWeekly(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var meal = await db.WeeklyMeals.FindAsync([id], ct);
        if (meal is null) return NotFound(new { error = "Meal not found" });
        if (meal.HouseholdId != user!.HouseholdId) return Forbid();

        db.WeeklyMeals.Remove(meal);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("api/households/{householdId:int}/meals/library")]
    public async Task<IActionResult> GetLibrary(int householdId, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != householdId) return Forbid();

        var meals = await db.MealLibrary
            .Where(m => m.HouseholdId == householdId)
            .Include(m => m.CreatedByUser)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        return Ok(meals.Select(ToLibraryResponse));
    }

    [HttpPost("api/households/{householdId:int}/meals/library")]
    public async Task<IActionResult> AddToLibrary(int householdId, [FromBody] AddLibraryMealRequest req, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        if (user!.HouseholdId != householdId) return Forbid();

        var meal = new MealLibrary
        {
            HouseholdId = householdId,
            Name = req.Name,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        db.MealLibrary.Add(meal);
        await db.SaveChangesAsync(ct);

        meal.CreatedByUser = user;
        return CreatedAtAction(nameof(GetLibrary), new { householdId }, ToLibraryResponse(meal));
    }

    [HttpDelete("api/meals/library/{id:int}")]
    public async Task<IActionResult> DeleteFromLibrary(int id, CancellationToken ct)
    {
        var (user, error) = await RequireUserAsync(currentUser, ct);
        if (error is not null) return error;

        var meal = await db.MealLibrary.FindAsync([id], ct);
        if (meal is null) return NotFound(new { error = "Library meal not found" });
        if (meal.HouseholdId != user!.HouseholdId) return Forbid();

        db.MealLibrary.Remove(meal);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
