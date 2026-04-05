using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface IMealService
{
    Task<ServiceResult<IReadOnlyList<WeeklyMealResponse>>> GetWeeklyAsync(Guid householdId, string? weekOf, User user, CancellationToken ct = default);
    Task<ServiceResult<WeeklyMealResponse>> AddWeeklyAsync(Guid householdId, AddWeeklyMealRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult<WeeklyMealResponse>> LinkToLibraryAsync(Guid weeklyMealId, PatchWeeklyMealRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult> DeleteWeeklyAsync(Guid id, User user, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<LibraryMealResponse>>> GetLibraryAsync(Guid householdId, User user, CancellationToken ct = default);
    Task<ServiceResult<LibraryMealResponse>> AddToLibraryAsync(Guid householdId, AddLibraryMealRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult> DeleteFromLibraryAsync(Guid id, User user, CancellationToken ct = default);
    Task<string?> GetLibraryImageAsync(Guid id, User user, CancellationToken ct = default);
    Task<ServiceResult> ToggleFavoriteAsync(Guid mealLibraryId, bool favorite, User user, CancellationToken ct = default);
    Task<ServiceResult<string>> RegenerateImageAsync(Guid mealId, User user, CancellationToken ct = default);
}
