using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Repositories;

public interface IMealRepository
{
    Task<List<WeeklyMeal>> GetWeeklyAsync(int householdId, DateOnly weekOf, CancellationToken ct);
    Task<WeeklyMeal> AddWeeklyAsync(WeeklyMeal meal, CancellationToken ct);
    Task<WeeklyMeal?> GetWeeklyByIdAsync(int id, CancellationToken ct);
    Task DeleteWeeklyAsync(WeeklyMeal meal, CancellationToken ct);
    Task<List<MealLibrary>> GetLibraryAsync(int householdId, CancellationToken ct);
    Task<MealLibrary> AddToLibraryAsync(MealLibrary meal, CancellationToken ct);
    Task<MealLibrary?> GetLibraryByIdAsync(int id, CancellationToken ct);
    Task DeleteFromLibraryAsync(MealLibrary meal, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
