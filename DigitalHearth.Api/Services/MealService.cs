using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;

namespace DigitalHearth.Api.Services;

public class MealService(IMealRepository meals, IHouseholdRepository households, IServiceScopeFactory scopeFactory, IImageGenerationService imageGeneration) : IMealService
{
    private static WeeklyMealResponse ToWeeklyResponse(WeeklyMeal m) =>
        new(m.Id, m.WeekOf.ToString("yyyy-MM-dd"), m.Name, m.MealLibraryId, m.MealLibraryId.HasValue, m.MealLibrary?.Image is not null, m.MealLibrary?.Image?.ImageToken);

    private static LibraryMealResponse ToLibraryResponse(MealLibrary m, HashSet<int>? favoriteIds = null) =>
        new(m.Id, m.Name, m.CreatedByUser.Username, m.CreatedAt, m.Tags, m.Image is not null, favoriteIds?.Contains(m.Id) ?? false, m.Image?.ImageToken);

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
            var household = await households.GetByIdAsync(householdId, ct);
            week = CurrentWeekStart(household!.WeekResetDay);
        }

        var mealList = await meals.GetWeeklyAsync(householdId, week, ct);

        return ServiceResult<IReadOnlyList<WeeklyMealResponse>>.Ok(mealList.Select(ToWeeklyResponse).ToList());
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
            var libEntry = await meals.GetLibraryByIdAsync(req.MealLibraryId.Value, ct);
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

        await meals.AddWeeklyAsync(meal, ct);

        return ServiceResult<WeeklyMealResponse>.Ok(ToWeeklyResponse(meal));
    }

    public async Task<ServiceResult> DeleteWeeklyAsync(int id, User user, CancellationToken ct = default)
    {
        var meal = await meals.GetWeeklyByIdAsync(id, ct);
        if (meal is null)
            return ServiceResult.NotFound("Meal not found");
        if (meal.HouseholdId != user.HouseholdId)
            return ServiceResult.Forbidden();

        await meals.DeleteWeeklyAsync(meal, ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IReadOnlyList<LibraryMealResponse>>> GetLibraryAsync(
        int householdId, User user, CancellationToken ct = default)
    {
        if (user.HouseholdId != householdId)
            return ServiceResult<IReadOnlyList<LibraryMealResponse>>.Forbidden();

        var mealList = await meals.GetLibraryAsync(householdId, ct);
        var favoriteIds = await meals.GetFavoriteIdsAsync(user.Id, householdId, ct);

        return ServiceResult<IReadOnlyList<LibraryMealResponse>>.Ok(mealList.Select(m => ToLibraryResponse(m, favoriteIds)).ToList());
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

        await meals.AddToLibraryAsync(meal, ct);
        meal.CreatedByUser = user;

        // Generate image in background — don't block the response
        var mealId = meal.Id;
        var mealName = meal.Name;
        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var scopedMeals = scope.ServiceProvider.GetRequiredService<IMealRepository>();
            var scopedHouseholds = scope.ServiceProvider.GetRequiredService<IHouseholdRepository>();
            var scopedImageGen = scope.ServiceProvider.GetRequiredService<IImageGenerationService>();

            var household = await scopedHouseholds.GetByIdAsync(householdId, CancellationToken.None);
            if (household is null || !CanGenerateImage(household)) return;

            var imageUrl = await scopedImageGen.GenerateImageAsync(mealName);
            if (imageUrl is not null)
            {
                IncrementImageGenCount(household);
                var saved = await scopedMeals.GetLibraryByIdAsync(mealId, CancellationToken.None);
                if (saved is not null)
                {
                    var now = DateTime.UtcNow;
                    if (saved.Image is null)
                        saved.Image = new Image
                        {
                            MealLibraryId = saved.Id,
                            ImageGuid = Guid.NewGuid(),
                            ImageToken = Guid.NewGuid().ToString("N"),
                            ImageData = imageUrl,
                            IsAiGenerated = true,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                    else
                    {
                        saved.Image.ImageToken = Guid.NewGuid().ToString("N");
                        saved.Image.ImageData = imageUrl;
                        saved.Image.UpdatedAt = now;
                    }
                    await scopedMeals.SaveAsync(CancellationToken.None);
                }
                await scopedHouseholds.SaveAsync(CancellationToken.None);
            }
        }, CancellationToken.None);

        return ServiceResult<LibraryMealResponse>.Ok(ToLibraryResponse(meal));
    }

    public async Task<string?> GetLibraryImageAsync(int id, User user, CancellationToken ct = default)
    {
        var meal = await meals.GetLibraryByIdAsync(id, ct);
        if (meal is null || meal.HouseholdId != user.HouseholdId) return null;
        return meal.Image?.ImageData;
    }

    public async Task<ServiceResult> DeleteFromLibraryAsync(int id, User user, CancellationToken ct = default)
    {
        var meal = await meals.GetLibraryByIdAsync(id, ct);
        if (meal is null)
            return ServiceResult.NotFound("Library meal not found");
        if (meal.HouseholdId != user.HouseholdId)
            return ServiceResult.Forbidden();

        await meals.DeleteFromLibraryAsync(meal, ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ToggleFavoriteAsync(int mealLibraryId, bool favorite, User user, CancellationToken ct = default)
    {
        var meal = await meals.GetLibraryByIdAsync(mealLibraryId, ct);
        if (meal is null || meal.HouseholdId != user.HouseholdId)
            return ServiceResult.NotFound("Library meal not found");

        if (favorite)
            await meals.FavoriteMealAsync(user.Id, mealLibraryId, ct);
        else
            await meals.UnfavoriteMealAsync(user.Id, mealLibraryId, ct);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<string>> RegenerateImageAsync(int mealId, User user, CancellationToken ct = default)
    {
        var meal = await meals.GetLibraryByIdAsync(mealId, ct);
        if (meal is null) return ServiceResult<string>.NotFound("Library meal not found");
        if (meal.HouseholdId != user.HouseholdId) return ServiceResult<string>.Forbidden();

        var household = await households.GetByIdAsync(user.HouseholdId, ct);
        if (household is null) return ServiceResult<string>.NotFound("Household not found");
        if (!CanGenerateImage(household))
            return ServiceResult<string>.BadRequest("Monthly image generation budget reached");

        var imageData = await imageGeneration.GenerateImageAsync(meal.Name, ct);
        if (imageData is null)
            return ServiceResult<string>.BadRequest("Image generation failed or is not configured");

        var token = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        IncrementImageGenCount(household);

        if (meal.Image is null)
            meal.Image = new Image
            {
                MealLibraryId = meal.Id,
                ImageGuid = Guid.NewGuid(),
                ImageToken = token,
                ImageData = imageData,
                IsAiGenerated = true,
                CreatedAt = now,
                UpdatedAt = now
            };
        else
        {
            meal.Image.ImageToken = token;
            meal.Image.ImageData = imageData;
            meal.Image.UpdatedAt = now;
        }

        await meals.SaveAsync(ct);

        return ServiceResult<string>.Ok(token);
    }

    private static bool CanGenerateImage(Household household)
    {
        if (household.MonthlyImageBudget is null) return true;
        var currentMonth = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        var count = household.ImageGenMonth == currentMonth ? household.ImageGenCount : 0;
        return count < household.MonthlyImageBudget;
    }

    private static void IncrementImageGenCount(Household household)
    {
        var currentMonth = DateTime.UtcNow.Year * 100 + DateTime.UtcNow.Month;
        if (household.ImageGenMonth != currentMonth)
        {
            household.ImageGenMonth = currentMonth;
            household.ImageGenCount = 0;
        }
        household.ImageGenCount++;
    }

    public async Task<ServiceResult<WeeklyMealResponse>> LinkToLibraryAsync(
        int weeklyMealId, PatchWeeklyMealRequest req, User user, CancellationToken ct = default)
    {
        var meal = await meals.GetWeeklyByIdAsync(weeklyMealId, ct);
        if (meal is null)
            return ServiceResult<WeeklyMealResponse>.NotFound("Weekly meal not found");
        if (meal.HouseholdId != user.HouseholdId)
            return ServiceResult<WeeklyMealResponse>.Forbidden();

        var libEntry = await meals.GetLibraryByIdAsync(req.MealLibraryId, ct);
        if (libEntry is null || libEntry.HouseholdId != user.HouseholdId)
            return ServiceResult<WeeklyMealResponse>.NotFound("Library meal not found");

        meal.MealLibraryId = libEntry.Id;
        await meals.SaveAsync(ct);

        return ServiceResult<WeeklyMealResponse>.Ok(ToWeeklyResponse(meal));
    }
}
