using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Services;

public class MealService(AppDbContext db, IServiceScopeFactory scopeFactory) : IMealService
{
    private static WeeklyMealResponse ToWeeklyResponse(WeeklyMeal m) =>
        new(m.Id, m.WeekOf.ToString("yyyy-MM-dd"), m.Name, m.MealLibraryId, m.MealLibraryId.HasValue, m.MealLibrary?.ImageUrl);

    private static LibraryMealResponse ToLibraryResponse(MealLibrary m) =>
        new(m.Id, m.Name, m.CreatedByUser.Username, m.CreatedAt, m.Tags, m.ImageUrl);

    private static DateOnly CurrentWeekStart(int weekResetDay)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var offset = ((int)today.DayOfWeek - weekResetDay + 7) % 7;
        return today.AddDays(-offset);
    }

    public async Task<ServiceResult<IReadOnlyList<WeeklyMealResponse>>> GetWeeklyAsync(
        int householdId, string? weekOf, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<IReadOnlyList<WeeklyMealResponse>>.Forbidden();

        DateOnly week;
        if (weekOf is not null && DateOnly.TryParse(weekOf, out var parsed))
        {
            week = parsed;
        }
        else
        {
            var household = await db.Households.FindAsync([householdId], ct);
            week = CurrentWeekStart(household!.WeekResetDay);
        }

        var meals = await db.WeeklyMeals
            .Where(m => m.HouseholdId == householdId && m.WeekOf == week)
            .Include(m => m.MealLibrary)
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<WeeklyMealResponse>>.Ok(meals.Select(ToWeeklyResponse).ToList());
    }

    public async Task<ServiceResult<WeeklyMealResponse>> AddWeeklyAsync(
        int householdId, AddWeeklyMealRequest req, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<WeeklyMealResponse>.Forbidden();

        if (!DateOnly.TryParse(req.WeekOf, out var weekOf))
            return ServiceResult<WeeklyMealResponse>.BadRequest("weekOf must be a valid date (YYYY-MM-DD)");

        string name;
        int? libraryId = null;

        if (req.MealLibraryId.HasValue)
        {
            var libEntry = await db.MealLibrary.FindAsync([req.MealLibraryId.Value], ct);
            if (libEntry is null || libEntry.HouseholdId != householdId)
                return ServiceResult<WeeklyMealResponse>.NotFound("Library meal not found");
            name = libEntry.Name;
            libraryId = libEntry.Id;
        }
        else if (!string.IsNullOrWhiteSpace(req.Name))
        {
            name = req.Name;
        }
        else
        {
            return ServiceResult<WeeklyMealResponse>.BadRequest("Either mealLibraryId or name is required");
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

        return ServiceResult<WeeklyMealResponse>.Ok(ToWeeklyResponse(meal));
    }

    public async Task<ServiceResult> DeleteWeeklyAsync(int id, User user, CancellationToken ct = default)
    {
        var meal = await db.WeeklyMeals.FindAsync([id], ct);
        if (meal is null)
            return ServiceResult.NotFound("Meal not found");
        if (meal.HouseholdId != user.HouseholdId)
            return ServiceResult.Forbidden();

        db.WeeklyMeals.Remove(meal);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IReadOnlyList<LibraryMealResponse>>> GetLibraryAsync(
        int householdId, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<IReadOnlyList<LibraryMealResponse>>.Forbidden();

        var meals = await db.MealLibrary
            .Where(m => m.HouseholdId == householdId)
            .Include(m => m.CreatedByUser)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<LibraryMealResponse>>.Ok(meals.Select(ToLibraryResponse).ToList());
    }

    public async Task<ServiceResult<LibraryMealResponse>> AddToLibraryAsync(
        int householdId, AddLibraryMealRequest req, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<LibraryMealResponse>.Forbidden();

        var meal = new MealLibrary
        {
            HouseholdId = householdId,
            Name = req.Name,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            Tags = req.Tags ?? []
        };

        db.MealLibrary.Add(meal);
        await db.SaveChangesAsync(ct);
        meal.CreatedByUser = user;

        // Generate image in background — don't block the response
        var mealId = meal.Id;
        var mealName = meal.Name;
        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scopedImageGen = scope.ServiceProvider.GetRequiredService<IImageGenerationService>();

            var imageUrl = await scopedImageGen.GenerateImageAsync(mealName);
            if (imageUrl is not null)
            {
                var saved = await scopedDb.MealLibrary.FindAsync(mealId);
                if (saved is not null)
                {
                    saved.ImageUrl = imageUrl;
                    await scopedDb.SaveChangesAsync();
                }
            }
        });

        return ServiceResult<LibraryMealResponse>.Ok(ToLibraryResponse(meal));
    }

    public async Task<ServiceResult> DeleteFromLibraryAsync(int id, User user, CancellationToken ct = default)
    {
        var meal = await db.MealLibrary.FindAsync([id], ct);
        if (meal is null)
            return ServiceResult.NotFound("Library meal not found");
        if (meal.HouseholdId != user.HouseholdId)
            return ServiceResult.Forbidden();

        db.MealLibrary.Remove(meal);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<WeeklyMealResponse>> LinkToLibraryAsync(
        int weeklyMealId, PatchWeeklyMealRequest req, User user, CancellationToken ct = default)
    {
        var meal = await db.WeeklyMeals.FindAsync([weeklyMealId], ct);
        if (meal is null)
            return ServiceResult<WeeklyMealResponse>.NotFound("Weekly meal not found");
        if (meal.HouseholdId != user.HouseholdId)
            return ServiceResult<WeeklyMealResponse>.Forbidden();

        var libEntry = await db.MealLibrary.FindAsync([req.MealLibraryId], ct);
        if (libEntry is null || libEntry.HouseholdId != user.HouseholdId)
            return ServiceResult<WeeklyMealResponse>.NotFound("Library meal not found");

        meal.MealLibraryId = libEntry.Id;
        await db.SaveChangesAsync(ct);

        return ServiceResult<WeeklyMealResponse>.Ok(ToWeeklyResponse(meal));
    }
}
