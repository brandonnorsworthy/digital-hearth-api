using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Models;

namespace DigitalHearth.Api.Services;

public interface IMealService
{
    Task<ServiceResult<IReadOnlyList<WeeklyMealResponse>>> GetWeeklyAsync(int householdId, string? weekOf, User user, CancellationToken ct = default);
    Task<ServiceResult<WeeklyMealResponse>> AddWeeklyAsync(int householdId, AddWeeklyMealRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult<WeeklyMealResponse>> LinkToLibraryAsync(int weeklyMealId, PatchWeeklyMealRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult> DeleteWeeklyAsync(int id, User user, CancellationToken ct = default);
    Task<ServiceResult<IReadOnlyList<LibraryMealResponse>>> GetLibraryAsync(int householdId, User user, CancellationToken ct = default);
    Task<ServiceResult<LibraryMealResponse>> AddToLibraryAsync(int householdId, AddLibraryMealRequest req, User user, CancellationToken ct = default);
    Task<ServiceResult> DeleteFromLibraryAsync(int id, User user, CancellationToken ct = default);
    Task<string?> GetLibraryImageAsync(int id, User user, CancellationToken ct = default);
}
