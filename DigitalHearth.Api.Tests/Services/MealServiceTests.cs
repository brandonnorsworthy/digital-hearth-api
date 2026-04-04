using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DigitalHearth.Api.Tests.Services;

public class MealServiceTests
{
    private readonly Mock<IMealRepository> _meals = new();
    private readonly Mock<IHouseholdRepository> _households = new();
    // Mocked as no-op — background image generation is out of scope for unit tests
    private readonly Mock<IServiceScopeFactory> _scopeFactory = new();
    private readonly MealService _sut;

    public MealServiceTests()
    {
        _sut = new MealService(_meals.Object, _households.Object, _scopeFactory.Object);
    }

    // --- GetWeekly ---

    [Fact]
    public async Task GetWeekly_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.GetWeeklyAsync(10, null, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task GetWeekly_WithExplicitWeekOf_UsesProvidedDate()
    {
        _meals.Setup(r => r.GetWeeklyAsync(10, new DateOnly(2025, 1, 6), default)).ReturnsAsync([]);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.GetWeeklyAsync(10, "2025-01-06", user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _meals.Verify(r => r.GetWeeklyAsync(10, new DateOnly(2025, 1, 6), default), Times.Once);
    }

    [Fact]
    public async Task GetWeekly_WithoutWeekOf_UsesHouseholdWeekResetDay()
    {
        var household = HouseholdFixtures.Default(id: 10, weekResetDay: 1); // Monday
        _households.Setup(r => r.GetByIdAsync(10, default)).ReturnsAsync(household);
        _meals.Setup(r => r.GetWeeklyAsync(10, It.IsAny<DateOnly>(), default)).ReturnsAsync([]);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.GetWeeklyAsync(10, null, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _households.Verify(r => r.GetByIdAsync(10, default), Times.Once);
    }

    [Fact]
    public async Task GetWeekly_ReturnsOkWithMappedResponses()
    {
        var weekly = MealFixtures.Weekly();
        _meals.Setup(r => r.GetWeeklyAsync(10, It.IsAny<DateOnly>(), default)).ReturnsAsync([weekly]);
        _households.Setup(r => r.GetByIdAsync(10, default)).ReturnsAsync(HouseholdFixtures.Default());
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.GetWeeklyAsync(10, null, user);

        result.Value!.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Pasta");
    }

    // --- AddWeekly ---

    [Fact]
    public async Task AddWeekly_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.AddWeeklyAsync(10, new AddWeeklyMealRequest("2025-01-06", null, "Pasta"), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task AddWeekly_InvalidWeekOfDate_ReturnsBadRequest()
    {
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.AddWeeklyAsync(10, new AddWeeklyMealRequest("not-a-date", null, "Pasta"), user);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    [Fact]
    public async Task AddWeekly_WithName_CreatesWeeklyMealUsingProvidedName()
    {
        _meals.Setup(r => r.AddWeeklyAsync(It.IsAny<WeeklyMeal>(), default))
            .ReturnsAsync((WeeklyMeal m, CancellationToken _) => m);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.AddWeeklyAsync(10, new AddWeeklyMealRequest("2025-01-06", null, "Pasta"), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("Pasta");
        result.Value.IsFromLibrary.Should().BeFalse();
    }

    [Fact]
    public async Task AddWeekly_WithMealLibraryId_UsesLibraryNameAndLinksId()
    {
        var libMeal = MealFixtures.LibraryMeal(id: 5, householdId: 10, name: "Tacos");
        _meals.Setup(r => r.GetLibraryByIdAsync(5, default)).ReturnsAsync(libMeal);
        _meals.Setup(r => r.AddWeeklyAsync(It.IsAny<WeeklyMeal>(), default))
            .ReturnsAsync((WeeklyMeal m, CancellationToken _) => m);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.AddWeeklyAsync(10, new AddWeeklyMealRequest("2025-01-06", 5, null), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("Tacos");
        result.Value.MealLibraryId.Should().Be(5);
        result.Value.IsFromLibrary.Should().BeTrue();
    }

    [Fact]
    public async Task AddWeekly_MealLibraryIdNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetLibraryByIdAsync(99, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.AddWeeklyAsync(10, new AddWeeklyMealRequest("2025-01-06", 99, null), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task AddWeekly_MealLibraryIdFromDifferentHousehold_ReturnsNotFound()
    {
        var libMeal = MealFixtures.LibraryMeal(id: 5, householdId: 99); // wrong household
        _meals.Setup(r => r.GetLibraryByIdAsync(5, default)).ReturnsAsync(libMeal);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.AddWeeklyAsync(10, new AddWeeklyMealRequest("2025-01-06", 5, null), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task AddWeekly_NeitherNameNorLibraryId_ReturnsBadRequest()
    {
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.AddWeeklyAsync(10, new AddWeeklyMealRequest("2025-01-06", null, null), user);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    // --- DeleteWeekly ---

    [Fact]
    public async Task DeleteWeekly_MealFound_ReturnsOk()
    {
        var meal = MealFixtures.Weekly(householdId: 10);
        _meals.Setup(r => r.GetWeeklyByIdAsync(1, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.DeleteWeeklyAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _meals.Verify(r => r.DeleteWeeklyAsync(meal, default), Times.Once);
    }

    [Fact]
    public async Task DeleteWeekly_MealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetWeeklyByIdAsync(1, default)).ReturnsAsync((WeeklyMeal?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.DeleteWeeklyAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteWeekly_MealInDifferentHousehold_ReturnsForbidden()
    {
        var meal = MealFixtures.Weekly(householdId: 10);
        _meals.Setup(r => r.GetWeeklyByIdAsync(1, default)).ReturnsAsync(meal);
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.DeleteWeeklyAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- GetLibrary ---

    [Fact]
    public async Task GetLibrary_UserInHousehold_ReturnsOk()
    {
        var user = UserFixtures.InHousehold(10);
        _meals.Setup(r => r.GetLibraryAsync(10, default)).ReturnsAsync([MealFixtures.LibraryMeal()]);
        _meals.Setup(r => r.GetFavoriteIdsAsync(user.Id, 10, default)).ReturnsAsync([]);

        var result = await _sut.GetLibraryAsync(10, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLibrary_FavoriteIdsIncludedInResponse()
    {
        var user = UserFixtures.InHousehold(10);
        var meal = MealFixtures.LibraryMeal(id: 5, householdId: 10);
        _meals.Setup(r => r.GetLibraryAsync(10, default)).ReturnsAsync([meal]);
        _meals.Setup(r => r.GetFavoriteIdsAsync(user.Id, 10, default)).ReturnsAsync([5]);

        var result = await _sut.GetLibraryAsync(10, user);

        result.Value![0].IsFavorited.Should().BeTrue();
    }

    [Fact]
    public async Task GetLibrary_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.GetLibraryAsync(10, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- AddToLibrary ---

    [Fact]
    public async Task AddToLibrary_ValidRequest_ReturnsOkWithEntry()
    {
        _meals.Setup(r => r.AddToLibraryAsync(It.IsAny<MealLibrary>(), default))
            .ReturnsAsync((MealLibrary m, CancellationToken _) => m);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.AddToLibraryAsync(10, new AddLibraryMealRequest("Tacos", ["Quick"]), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("Tacos");
    }

    [Fact]
    public async Task AddToLibrary_SetsCreatedByUserId()
    {
        _meals.Setup(r => r.AddToLibraryAsync(It.IsAny<MealLibrary>(), default))
            .ReturnsAsync((MealLibrary m, CancellationToken _) => m);
        var user = UserFixtures.Member(id: 7);

        await _sut.AddToLibraryAsync(10, new AddLibraryMealRequest("Tacos", null), user);

        _meals.Verify(r => r.AddToLibraryAsync(
            It.Is<MealLibrary>(m => m.CreatedByUserId == 7), default), Times.Once);
    }

    [Fact]
    public async Task AddToLibrary_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.AddToLibraryAsync(10, new AddLibraryMealRequest("Tacos", null), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- DeleteFromLibrary ---

    [Fact]
    public async Task DeleteFromLibrary_MealFound_ReturnsOk()
    {
        var meal = MealFixtures.LibraryMeal(householdId: 10);
        _meals.Setup(r => r.GetLibraryByIdAsync(1, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.DeleteFromLibraryAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _meals.Verify(r => r.DeleteFromLibraryAsync(meal, default), Times.Once);
    }

    [Fact]
    public async Task DeleteFromLibrary_MealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetLibraryByIdAsync(1, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.DeleteFromLibraryAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteFromLibrary_MealInDifferentHousehold_ReturnsForbidden()
    {
        var meal = MealFixtures.LibraryMeal(householdId: 10);
        _meals.Setup(r => r.GetLibraryByIdAsync(1, default)).ReturnsAsync(meal);
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.DeleteFromLibraryAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- LinkToLibrary ---

    [Fact]
    public async Task LinkToLibrary_ValidIds_LinksAndReturnsOk()
    {
        var weekly = MealFixtures.Weekly(id: 1, householdId: 10);
        var lib = MealFixtures.LibraryMeal(id: 5, householdId: 10);
        _meals.Setup(r => r.GetWeeklyByIdAsync(1, default)).ReturnsAsync(weekly);
        _meals.Setup(r => r.GetLibraryByIdAsync(5, default)).ReturnsAsync(lib);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.LinkToLibraryAsync(1, new PatchWeeklyMealRequest(5), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.MealLibraryId.Should().Be(5);
        result.Value.IsFromLibrary.Should().BeTrue();
    }

    [Fact]
    public async Task LinkToLibrary_WeeklyMealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetWeeklyByIdAsync(1, default)).ReturnsAsync((WeeklyMeal?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.LinkToLibraryAsync(1, new PatchWeeklyMealRequest(5), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task LinkToLibrary_WeeklyMealInDifferentHousehold_ReturnsForbidden()
    {
        var weekly = MealFixtures.Weekly(householdId: 10);
        _meals.Setup(r => r.GetWeeklyByIdAsync(1, default)).ReturnsAsync(weekly);
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.LinkToLibraryAsync(1, new PatchWeeklyMealRequest(5), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task LinkToLibrary_LibraryMealNotFound_ReturnsNotFound()
    {
        var weekly = MealFixtures.Weekly(householdId: 10);
        _meals.Setup(r => r.GetWeeklyByIdAsync(1, default)).ReturnsAsync(weekly);
        _meals.Setup(r => r.GetLibraryByIdAsync(99, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.LinkToLibraryAsync(1, new PatchWeeklyMealRequest(99), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task LinkToLibrary_LibraryMealInDifferentHousehold_ReturnsNotFound()
    {
        var weekly = MealFixtures.Weekly(householdId: 10);
        var lib = MealFixtures.LibraryMeal(id: 5, householdId: 99); // wrong household
        _meals.Setup(r => r.GetWeeklyByIdAsync(1, default)).ReturnsAsync(weekly);
        _meals.Setup(r => r.GetLibraryByIdAsync(5, default)).ReturnsAsync(lib);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.LinkToLibraryAsync(1, new PatchWeeklyMealRequest(5), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    // --- ToggleFavorite ---

    [Fact]
    public async Task ToggleFavorite_FavoriteTrue_CallsFavoriteMeal()
    {
        var meal = MealFixtures.LibraryMeal(id: 5, householdId: 10);
        _meals.Setup(r => r.GetLibraryByIdAsync(5, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.ToggleFavoriteAsync(5, true, user);

        result.IsSuccess.Should().BeTrue();
        _meals.Verify(r => r.FavoriteMealAsync(user.Id, 5, default), Times.Once);
    }

    [Fact]
    public async Task ToggleFavorite_FavoriteFalse_CallsUnfavoriteMeal()
    {
        var meal = MealFixtures.LibraryMeal(id: 5, householdId: 10);
        _meals.Setup(r => r.GetLibraryByIdAsync(5, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.ToggleFavoriteAsync(5, false, user);

        result.IsSuccess.Should().BeTrue();
        _meals.Verify(r => r.UnfavoriteMealAsync(user.Id, 5, default), Times.Once);
    }

    [Fact]
    public async Task ToggleFavorite_MealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetLibraryByIdAsync(99, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.ToggleFavoriteAsync(99, true, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task ToggleFavorite_MealInDifferentHousehold_ReturnsNotFound()
    {
        var meal = MealFixtures.LibraryMeal(id: 5, householdId: 99);
        _meals.Setup(r => r.GetLibraryByIdAsync(5, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.ToggleFavoriteAsync(5, true, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }
}
