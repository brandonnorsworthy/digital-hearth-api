using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Repositories;

public interface IMealRepository
{
    Task<List<WeeklyMeal>> GetWeeklyAsync(Guid householdId, DateOnly weekOf, CancellationToken ct);
    Task<WeeklyMeal> AddWeeklyAsync(WeeklyMeal meal, CancellationToken ct);
    Task<WeeklyMeal?> GetWeeklyByIdAsync(Guid id, CancellationToken ct);
    Task DeleteWeeklyAsync(WeeklyMeal meal, CancellationToken ct);
    Task<List<MealLibrary>> GetLibraryAsync(Guid householdId, CancellationToken ct);
    Task<MealLibrary> AddToLibraryAsync(MealLibrary meal, CancellationToken ct);
    Task<MealLibrary?> GetLibraryByIdAsync(Guid id, CancellationToken ct);
    Task DeleteFromLibraryAsync(MealLibrary meal, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task<HashSet<Guid>> GetFavoriteIdsAsync(Guid userId, Guid householdId, CancellationToken ct);
    Task FavoriteMealAsync(Guid userId, Guid mealLibraryId, CancellationToken ct);
    Task UnfavoriteMealAsync(Guid userId, Guid mealLibraryId, CancellationToken ct);
}
