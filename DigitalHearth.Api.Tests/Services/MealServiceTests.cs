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
    private readonly Mock<IImageGenerationService> _imageGen = new();
    private readonly MealService _sut;

    private static readonly Guid HouseholdId = UserFixtures.DefaultHouseholdId;
    private static readonly Guid OutsideHouseholdId = UserFixtures.OutsideHouseholdId;
    private static readonly Guid MealId = MealFixtures.DefaultMealId;
    private static readonly Guid LibMealId = new("00000000-0000-0000-0000-000000000005");
    private static readonly Guid MissingId = new("00000000-0000-0000-0000-000000000099");

    public MealServiceTests()
    {
        _sut = new MealService(_meals.Object, _households.Object, _scopeFactory.Object, _imageGen.Object);
    }

    // --- GetWeekly ---

    [Fact]
    public async Task GetWeekly_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.GetWeeklyAsync(HouseholdId, null, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task GetWeekly_WithExplicitWeekOf_UsesProvidedDate()
    {
        _meals.Setup(r => r.GetWeeklyAsync(HouseholdId, new DateOnly(2025, 1, 6), default)).ReturnsAsync([]);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.GetWeeklyAsync(HouseholdId, "2025-01-06", user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _meals.Verify(r => r.GetWeeklyAsync(HouseholdId, new DateOnly(2025, 1, 6), default), Times.Once);
    }

    [Fact]
    public async Task GetWeekly_WithoutWeekOf_UsesHouseholdWeekResetDay()
    {
        var household = HouseholdFixtures.Default(weekResetDay: 1); // Monday
        _households.Setup(r => r.GetByIdAsync(HouseholdId, default)).ReturnsAsync(household);
        _meals.Setup(r => r.GetWeeklyAsync(HouseholdId, It.IsAny<DateOnly>(), default)).ReturnsAsync([]);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.GetWeeklyAsync(HouseholdId, null, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _households.Verify(r => r.GetByIdAsync(HouseholdId, default), Times.Once);
    }

    [Fact]
    public async Task GetWeekly_ReturnsOkWithMappedResponses()
    {
        var weekly = MealFixtures.Weekly();
        _meals.Setup(r => r.GetWeeklyAsync(HouseholdId, It.IsAny<DateOnly>(), default)).ReturnsAsync([weekly]);
        _households.Setup(r => r.GetByIdAsync(HouseholdId, default)).ReturnsAsync(HouseholdFixtures.Default());
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.GetWeeklyAsync(HouseholdId, null, user);

        result.Value!.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Pasta");
    }

    // --- AddWeekly ---

    [Fact]
    public async Task AddWeekly_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.AddWeeklyAsync(HouseholdId, new AddWeeklyMealRequest("2025-01-06", null, "Pasta"), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task AddWeekly_InvalidWeekOfDate_ReturnsBadRequest()
    {
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.AddWeeklyAsync(HouseholdId, new AddWeeklyMealRequest("not-a-date", null, "Pasta"), user);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    [Fact]
    public async Task AddWeekly_WithName_CreatesWeeklyMealUsingProvidedName()
    {
        _meals.Setup(r => r.AddWeeklyAsync(It.IsAny<WeeklyMeal>(), default))
            .ReturnsAsync((WeeklyMeal m, CancellationToken _) => m);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.AddWeeklyAsync(HouseholdId, new AddWeeklyMealRequest("2025-01-06", null, "Pasta"), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("Pasta");
        result.Value.IsFromLibrary.Should().BeFalse();
    }

    [Fact]
    public async Task AddWeekly_WithMealLibraryId_UsesLibraryNameAndLinksId()
    {
        var libMeal = MealFixtures.LibraryMeal(id: LibMealId, householdId: HouseholdId, name: "Tacos");
        _meals.Setup(r => r.GetLibraryByIdAsync(LibMealId, default)).ReturnsAsync(libMeal);
        _meals.Setup(r => r.AddWeeklyAsync(It.IsAny<WeeklyMeal>(), default))
            .ReturnsAsync((WeeklyMeal m, CancellationToken _) => m);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.AddWeeklyAsync(HouseholdId, new AddWeeklyMealRequest("2025-01-06", LibMealId, null), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("Tacos");
        result.Value.MealLibraryId.Should().Be(LibMealId);
        result.Value.IsFromLibrary.Should().BeTrue();
    }

    [Fact]
    public async Task AddWeekly_MealLibraryIdNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetLibraryByIdAsync(MissingId, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.AddWeeklyAsync(HouseholdId, new AddWeeklyMealRequest("2025-01-06", MissingId, null), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task AddWeekly_MealLibraryIdFromDifferentHousehold_ReturnsNotFound()
    {
        var libMeal = MealFixtures.LibraryMeal(id: LibMealId, householdId: OutsideHouseholdId); // wrong household
        _meals.Setup(r => r.GetLibraryByIdAsync(LibMealId, default)).ReturnsAsync(libMeal);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.AddWeeklyAsync(HouseholdId, new AddWeeklyMealRequest("2025-01-06", LibMealId, null), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task AddWeekly_NeitherNameNorLibraryId_ReturnsBadRequest()
    {
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.AddWeeklyAsync(HouseholdId, new AddWeeklyMealRequest("2025-01-06", null, null), user);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    // --- DeleteWeekly ---

    [Fact]
    public async Task DeleteWeekly_MealFound_ReturnsOk()
    {
        var meal = MealFixtures.Weekly(householdId: HouseholdId);
        _meals.Setup(r => r.GetWeeklyByIdAsync(MealId, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.DeleteWeeklyAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _meals.Verify(r => r.DeleteWeeklyAsync(meal, default), Times.Once);
    }

    [Fact]
    public async Task DeleteWeekly_MealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetWeeklyByIdAsync(MealId, default)).ReturnsAsync((WeeklyMeal?)null);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.DeleteWeeklyAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteWeekly_MealInDifferentHousehold_ReturnsForbidden()
    {
        var meal = MealFixtures.Weekly(householdId: HouseholdId);
        _meals.Setup(r => r.GetWeeklyByIdAsync(MealId, default)).ReturnsAsync(meal);
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.DeleteWeeklyAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- GetLibrary ---

    [Fact]
    public async Task GetLibrary_UserInHousehold_ReturnsOk()
    {
        var user = UserFixtures.InHousehold(HouseholdId);
        _meals.Setup(r => r.GetLibraryAsync(HouseholdId, default)).ReturnsAsync([MealFixtures.LibraryMeal()]);
        _meals.Setup(r => r.GetFavoriteIdsAsync(user.Id, HouseholdId, default)).ReturnsAsync([]);

        var result = await _sut.GetLibraryAsync(HouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLibrary_FavoriteIdsIncludedInResponse()
    {
        var user = UserFixtures.InHousehold(HouseholdId);
        var meal = MealFixtures.LibraryMeal(id: LibMealId, householdId: HouseholdId);
        _meals.Setup(r => r.GetLibraryAsync(HouseholdId, default)).ReturnsAsync([meal]);
        _meals.Setup(r => r.GetFavoriteIdsAsync(user.Id, HouseholdId, default)).ReturnsAsync([LibMealId]);

        var result = await _sut.GetLibraryAsync(HouseholdId, user);

        result.Value![0].IsFavorited.Should().BeTrue();
    }

    [Fact]
    public async Task GetLibrary_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.GetLibraryAsync(HouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- AddToLibrary ---

    [Fact]
    public async Task AddToLibrary_ValidRequest_ReturnsOkWithEntry()
    {
        _meals.Setup(r => r.AddToLibraryAsync(It.IsAny<MealLibrary>(), default))
            .ReturnsAsync((MealLibrary m, CancellationToken _) => m);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.AddToLibraryAsync(HouseholdId, new AddLibraryMealRequest("Tacos", ["Quick"]), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("Tacos");
    }

    [Fact]
    public async Task AddToLibrary_SetsCreatedByUserId()
    {
        _meals.Setup(r => r.AddToLibraryAsync(It.IsAny<MealLibrary>(), default))
            .ReturnsAsync((MealLibrary m, CancellationToken _) => m);
        var userId = new Guid("00000000-0000-0000-0000-000000000007");
        var user = UserFixtures.Member(id: userId);

        await _sut.AddToLibraryAsync(HouseholdId, new AddLibraryMealRequest("Tacos", null), user);

        _meals.Verify(r => r.AddToLibraryAsync(
            It.Is<MealLibrary>(m => m.CreatedByUserId == userId), default), Times.Once);
    }

    [Fact]
    public async Task AddToLibrary_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.AddToLibraryAsync(HouseholdId, new AddLibraryMealRequest("Tacos", null), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- DeleteFromLibrary ---

    [Fact]
    public async Task DeleteFromLibrary_MealFound_ReturnsOk()
    {
        var meal = MealFixtures.LibraryMeal(householdId: HouseholdId);
        _meals.Setup(r => r.GetLibraryByIdAsync(MealId, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.DeleteFromLibraryAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _meals.Verify(r => r.DeleteFromLibraryAsync(meal, default), Times.Once);
    }

    [Fact]
    public async Task DeleteFromLibrary_MealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetLibraryByIdAsync(MealId, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.DeleteFromLibraryAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteFromLibrary_MealInDifferentHousehold_ReturnsForbidden()
    {
        var meal = MealFixtures.LibraryMeal(householdId: HouseholdId);
        _meals.Setup(r => r.GetLibraryByIdAsync(MealId, default)).ReturnsAsync(meal);
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.DeleteFromLibraryAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- LinkToLibrary ---

    [Fact]
    public async Task LinkToLibrary_ValidIds_LinksAndReturnsOk()
    {
        var weekly = MealFixtures.Weekly(id: MealId, householdId: HouseholdId);
        var lib = MealFixtures.LibraryMeal(id: LibMealId, householdId: HouseholdId);
        _meals.Setup(r => r.GetWeeklyByIdAsync(MealId, default)).ReturnsAsync(weekly);
        _meals.Setup(r => r.GetLibraryByIdAsync(LibMealId, default)).ReturnsAsync(lib);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.LinkToLibraryAsync(MealId, new PatchWeeklyMealRequest(LibMealId), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.MealLibraryId.Should().Be(LibMealId);
        result.Value.IsFromLibrary.Should().BeTrue();
    }

    [Fact]
    public async Task LinkToLibrary_WeeklyMealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetWeeklyByIdAsync(MealId, default)).ReturnsAsync((WeeklyMeal?)null);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.LinkToLibraryAsync(MealId, new PatchWeeklyMealRequest(LibMealId), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task LinkToLibrary_WeeklyMealInDifferentHousehold_ReturnsForbidden()
    {
        var weekly = MealFixtures.Weekly(householdId: HouseholdId);
        _meals.Setup(r => r.GetWeeklyByIdAsync(MealId, default)).ReturnsAsync(weekly);
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.LinkToLibraryAsync(MealId, new PatchWeeklyMealRequest(LibMealId), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task LinkToLibrary_LibraryMealNotFound_ReturnsNotFound()
    {
        var weekly = MealFixtures.Weekly(householdId: HouseholdId);
        _meals.Setup(r => r.GetWeeklyByIdAsync(MealId, default)).ReturnsAsync(weekly);
        _meals.Setup(r => r.GetLibraryByIdAsync(MissingId, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.LinkToLibraryAsync(MealId, new PatchWeeklyMealRequest(MissingId), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task LinkToLibrary_LibraryMealInDifferentHousehold_ReturnsNotFound()
    {
        var weekly = MealFixtures.Weekly(householdId: HouseholdId);
        var lib = MealFixtures.LibraryMeal(id: LibMealId, householdId: OutsideHouseholdId); // wrong household
        _meals.Setup(r => r.GetWeeklyByIdAsync(MealId, default)).ReturnsAsync(weekly);
        _meals.Setup(r => r.GetLibraryByIdAsync(LibMealId, default)).ReturnsAsync(lib);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.LinkToLibraryAsync(MealId, new PatchWeeklyMealRequest(LibMealId), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    // --- ToggleFavorite ---

    [Fact]
    public async Task ToggleFavorite_FavoriteTrue_CallsFavoriteMeal()
    {
        var meal = MealFixtures.LibraryMeal(id: LibMealId, householdId: HouseholdId);
        _meals.Setup(r => r.GetLibraryByIdAsync(LibMealId, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.ToggleFavoriteAsync(LibMealId, true, user);

        result.IsSuccess.Should().BeTrue();
        _meals.Verify(r => r.FavoriteMealAsync(user.Id, LibMealId, default), Times.Once);
    }

    [Fact]
    public async Task ToggleFavorite_FavoriteFalse_CallsUnfavoriteMeal()
    {
        var meal = MealFixtures.LibraryMeal(id: LibMealId, householdId: HouseholdId);
        _meals.Setup(r => r.GetLibraryByIdAsync(LibMealId, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.ToggleFavoriteAsync(LibMealId, false, user);

        result.IsSuccess.Should().BeTrue();
        _meals.Verify(r => r.UnfavoriteMealAsync(user.Id, LibMealId, default), Times.Once);
    }

    [Fact]
    public async Task ToggleFavorite_MealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetLibraryByIdAsync(MissingId, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.ToggleFavoriteAsync(MissingId, true, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task ToggleFavorite_MealInDifferentHousehold_ReturnsNotFound()
    {
        var meal = MealFixtures.LibraryMeal(id: LibMealId, householdId: OutsideHouseholdId);
        _meals.Setup(r => r.GetLibraryByIdAsync(LibMealId, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.ToggleFavoriteAsync(LibMealId, true, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    // --- RegenerateImage ---

    [Fact]
    public async Task RegenerateImage_MealNotFound_ReturnsNotFound()
    {
        _meals.Setup(r => r.GetLibraryByIdAsync(MealId, default)).ReturnsAsync((MealLibrary?)null);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.RegenerateImageAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task RegenerateImage_MealInDifferentHousehold_ReturnsForbidden()
    {
        var meal = MealFixtures.LibraryMeal(id: MealId, householdId: OutsideHouseholdId);
        _meals.Setup(r => r.GetLibraryByIdAsync(MealId, default)).ReturnsAsync(meal);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.RegenerateImageAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task RegenerateImage_ImageGenFails_ReturnsBadRequest()
    {
        var meal = MealFixtures.LibraryMeal(id: MealId, householdId: HouseholdId, name: "Pasta");
        _meals.Setup(r => r.GetLibraryByIdAsync(MealId, It.IsAny<CancellationToken>())).ReturnsAsync(meal);
        _households.Setup(r => r.GetByIdAsync(HouseholdId, It.IsAny<CancellationToken>())).ReturnsAsync(HouseholdFixtures.Default());
        _imageGen.Setup(s => s.GenerateImageAsync("Pasta", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.RegenerateImageAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    [Fact]
    public async Task RegenerateImage_Success_SavesImageDataAndReturnsOk()
    {
        var meal = MealFixtures.LibraryMeal(id: MealId, householdId: HouseholdId, name: "Pasta");
        _meals.Setup(r => r.GetLibraryByIdAsync(MealId, It.IsAny<CancellationToken>())).ReturnsAsync(meal);
        _households.Setup(r => r.GetByIdAsync(HouseholdId, It.IsAny<CancellationToken>())).ReturnsAsync(HouseholdFixtures.Default());
        _imageGen.Setup(s => s.GenerateImageAsync("Pasta", It.IsAny<CancellationToken>())).ReturnsAsync("data:image/png;base64,abc");
        var user = UserFixtures.InHousehold(HouseholdId);

        var result = await _sut.RegenerateImageAsync(MealId, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value.Should().NotBeNullOrEmpty();
        meal.Image!.ImageData.Should().Be("data:image/png;base64,abc");
        meal.Image.ImageGuid.Should().NotBeEmpty();
        _meals.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
