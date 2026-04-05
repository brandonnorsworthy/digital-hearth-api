using DigitalHearth.Api.Data;
using DigitalHearth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalHearth.Api.Repositories;

public class MealRepository(AppDbContext db) : IMealRepository
{
    public async Task<List<WeeklyMeal>> GetWeeklyAsync(Guid householdId, DateOnly weekOf, CancellationToken ct)
    {
        return await db.WeeklyMeals
            .Where(m => m.HouseholdId == householdId && m.WeekOf == weekOf)
            .Include(m => m.MealLibrary).ThenInclude(l => l!.Image)
            .ToListAsync(ct);
    }

    public async Task<WeeklyMeal> AddWeeklyAsync(WeeklyMeal meal, CancellationToken ct)
    {
        db.WeeklyMeals.Add(meal);
        await db.SaveChangesAsync(ct);
        return meal;
    }

    public async Task<WeeklyMeal?> GetWeeklyByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.WeeklyMeals
            .Include(m => m.MealLibrary).ThenInclude(l => l!.Image)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task DeleteWeeklyAsync(WeeklyMeal meal, CancellationToken ct)
    {
        db.WeeklyMeals.Remove(meal);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<MealLibrary>> GetLibraryAsync(Guid householdId, CancellationToken ct)
    {
        return await db.MealLibrary
            .Where(m => m.HouseholdId == householdId)
            .Include(m => m.CreatedByUser)
            .Include(m => m.Image)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<MealLibrary> AddToLibraryAsync(MealLibrary meal, CancellationToken ct)
    {
        db.MealLibrary.Add(meal);
        await db.SaveChangesAsync(ct);
        return meal;
    }

    public async Task<MealLibrary?> GetLibraryByIdAsync(Guid id, CancellationToken ct)
    {
        return await db.MealLibrary
            .Include(m => m.Image)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task DeleteFromLibraryAsync(MealLibrary meal, CancellationToken ct)
    {
        db.MealLibrary.Remove(meal);
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task<HashSet<Guid>> GetFavoriteIdsAsync(Guid userId, Guid householdId, CancellationToken ct)
    {
        var ids = await db.MealFavorites
            .Where(f => f.UserId == userId && f.MealLibrary.HouseholdId == householdId)
            .Select(f => f.MealLibraryId)
            .ToListAsync(ct);
        return [.. ids];
    }

    public async Task FavoriteMealAsync(Guid userId, Guid mealLibraryId, CancellationToken ct)
    {
        var exists = await db.MealFavorites
            .AnyAsync(f => f.UserId == userId && f.MealLibraryId == mealLibraryId, ct);
        if (exists) return;

        db.MealFavorites.Add(new Models.MealFavorite { UserId = userId, MealLibraryId = mealLibraryId });
        await db.SaveChangesAsync(ct);
    }

    public async Task UnfavoriteMealAsync(Guid userId, Guid mealLibraryId, CancellationToken ct)
    {
        await db.MealFavorites
            .Where(f => f.UserId == userId && f.MealLibraryId == mealLibraryId)
            .ExecuteDeleteAsync(ct);
    }
}
