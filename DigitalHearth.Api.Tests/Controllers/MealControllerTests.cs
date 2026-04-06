using DigitalHearth.Api.Controllers;
using DigitalHearth.Api.Data;
using DigitalHearth.Api.DTOs.Meal;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DigitalHearth.Api.Tests.Controllers;

public class MealControllerTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IMealService> _mealService = new();
    private readonly Mock<IImageGenerationService> _imageGen = new();
    private readonly MealController _sut;

    private static readonly WeeklyMealResponse FakeWeekly = new(MealFixtures.DefaultMealId, "2025-01-06", "Pasta", null, false, false, null, false);
    private static readonly LibraryMealResponse FakeLibrary = new(MealFixtures.DefaultMealId, "Pasta", "alice", DateTime.UtcNow, [], false, false, null);

    public MealControllerTests()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _sut = new MealController(_currentUser.Object, _mealService.Object, _imageGen.Object, db);
        _sut.ControllerContext = new ControllerContext();
    }

    // --- GenerateImage ---

    [Fact]
    public async Task GenerateImage_Authenticated_ImageGenSucceeds_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _imageGen.Setup(s => s.GenerateImageAsync("Pasta", default)).ReturnsAsync("data:image/png;base64,abc");

        var result = await _sut.GenerateImage(new GenerateImageRequest("Pasta"), default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GenerateImage_Authenticated_ImageGenFails_Returns400()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _imageGen.Setup(s => s.GenerateImageAsync(It.IsAny<string>(), default)).ReturnsAsync((string?)null);

        var result = await _sut.GenerateImage(new GenerateImageRequest("Pasta"), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateImage_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.GenerateImage(new GenerateImageRequest("Pasta"), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- GetWeekly ---

    [Fact]
    public async Task GetWeekly_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.GetWeeklyAsync(UserFixtures.DefaultHouseholdId, null, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<IReadOnlyList<WeeklyMealResponse>>.Ok([FakeWeekly]));

        var result = await _sut.GetWeekly(UserFixtures.DefaultHouseholdId, null, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetWeekly_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.GetWeekly(UserFixtures.DefaultHouseholdId, null, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetWeekly_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.GetWeeklyAsync(UserFixtures.DefaultHouseholdId, null, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<IReadOnlyList<WeeklyMealResponse>>.Forbidden());

        var result = await _sut.GetWeekly(UserFixtures.DefaultHouseholdId, null, default);

        result.Should().BeOfType<ForbidResult>();
    }

    // --- AddWeekly ---

    [Fact]
    public async Task AddWeekly_ServiceReturnsOk_Returns201()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.AddWeeklyAsync(UserFixtures.DefaultHouseholdId, It.IsAny<AddWeeklyMealRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<WeeklyMealResponse>.Ok(FakeWeekly));

        var result = await _sut.AddWeekly(UserFixtures.DefaultHouseholdId, new AddWeeklyMealRequest("2025-01-06", null, "Pasta"), default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(FakeWeekly);
    }

    [Fact]
    public async Task AddWeekly_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.AddWeekly(UserFixtures.DefaultHouseholdId, new AddWeeklyMealRequest("2025-01-06", null, "Pasta"), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task AddWeekly_ServiceReturnsBadRequest_Returns400()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.AddWeeklyAsync(UserFixtures.DefaultHouseholdId, It.IsAny<AddWeeklyMealRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<WeeklyMealResponse>.BadRequest("Either mealLibraryId or name is required"));

        var result = await _sut.AddWeekly(UserFixtures.DefaultHouseholdId, new AddWeeklyMealRequest("2025-01-06", null, null), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // --- PatchWeekly ---

    [Fact]
    public async Task PatchWeekly_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.LinkToLibraryAsync(MealFixtures.DefaultMealId, It.IsAny<PatchWeeklyMealRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<WeeklyMealResponse>.Ok(FakeWeekly));

        var result = await _sut.PatchWeekly(MealFixtures.DefaultMealId, new PatchWeeklyMealRequest(MealFixtures.DefaultMealId), default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task PatchWeekly_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.PatchWeekly(MealFixtures.DefaultMealId, new PatchWeeklyMealRequest(MealFixtures.DefaultMealId), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task PatchWeekly_ServiceReturnsNotFound_Returns404()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.LinkToLibraryAsync(MealFixtures.DefaultMealId, It.IsAny<PatchWeeklyMealRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<WeeklyMealResponse>.NotFound("Weekly meal not found"));

        var result = await _sut.PatchWeekly(MealFixtures.DefaultMealId, new PatchWeeklyMealRequest(MealFixtures.DefaultMealId), default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // --- DeleteWeekly ---

    [Fact]
    public async Task DeleteWeekly_Authenticated_ServiceReturnsOk_Returns204()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.DeleteWeeklyAsync(MealFixtures.DefaultMealId, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.DeleteWeekly(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteWeekly_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.DeleteWeekly(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task DeleteWeekly_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.DeleteWeeklyAsync(MealFixtures.DefaultMealId, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Forbidden());

        var result = await _sut.DeleteWeekly(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<ForbidResult>();
    }

    // --- GetLibrary ---

    [Fact]
    public async Task GetLibrary_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.GetLibraryAsync(UserFixtures.DefaultHouseholdId, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<IReadOnlyList<LibraryMealResponse>>.Ok([FakeLibrary]));

        var result = await _sut.GetLibrary(UserFixtures.DefaultHouseholdId, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLibrary_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.GetLibrary(UserFixtures.DefaultHouseholdId, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- AddToLibrary ---

    [Fact]
    public async Task AddToLibrary_ServiceReturnsOk_Returns201()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.AddToLibraryAsync(UserFixtures.DefaultHouseholdId, It.IsAny<AddLibraryMealRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<LibraryMealResponse>.Ok(FakeLibrary));

        var result = await _sut.AddToLibrary(UserFixtures.DefaultHouseholdId, new AddLibraryMealRequest("Pasta", null), default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(FakeLibrary);
    }

    [Fact]
    public async Task AddToLibrary_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.AddToLibrary(UserFixtures.DefaultHouseholdId, new AddLibraryMealRequest("Pasta", null), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task AddToLibrary_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.AddToLibraryAsync(UserFixtures.DefaultHouseholdId, It.IsAny<AddLibraryMealRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<LibraryMealResponse>.Forbidden());

        var result = await _sut.AddToLibrary(UserFixtures.DefaultHouseholdId, new AddLibraryMealRequest("Pasta", null), default);

        result.Should().BeOfType<ForbidResult>();
    }

    // --- DeleteFromLibrary ---

    [Fact]
    public async Task DeleteFromLibrary_Authenticated_ServiceReturnsOk_Returns204()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.DeleteFromLibraryAsync(MealFixtures.DefaultMealId, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.DeleteFromLibrary(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteFromLibrary_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.DeleteFromLibrary(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task DeleteFromLibrary_ServiceReturnsNotFound_Returns404()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.DeleteFromLibraryAsync(MealFixtures.DefaultMealId, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.NotFound("Library meal not found"));

        var result = await _sut.DeleteFromLibrary(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // --- FavoriteMeal ---

    [Fact]
    public async Task FavoriteMeal_Authenticated_ServiceReturnsOk_Returns204()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.ToggleFavoriteAsync(MealFixtures.DefaultMealId, true, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.FavoriteMeal(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task FavoriteMeal_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.FavoriteMeal(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task FavoriteMeal_ServiceReturnsNotFound_Returns404()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.ToggleFavoriteAsync(MealFixtures.DefaultMealId, true, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.NotFound("Library meal not found"));

        var result = await _sut.FavoriteMeal(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // --- UnfavoriteMeal ---

    [Fact]
    public async Task UnfavoriteMeal_Authenticated_ServiceReturnsOk_Returns204()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.ToggleFavoriteAsync(MealFixtures.DefaultMealId, false, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.UnfavoriteMeal(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UnfavoriteMeal_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.UnfavoriteMeal(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- RegenerateImage ---

    [Fact]
    public async Task RegenerateImage_Authenticated_ServiceReturnsOk_Returns200WithToken()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.RegenerateImageAsync(MealFixtures.DefaultMealId, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<string>.Ok("abc123token"));

        var result = await _sut.RegenerateImage(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RegenerateImage_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.RegenerateImage(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task RegenerateImage_ServiceReturnsNotFound_Returns404()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.RegenerateImageAsync(MealFixtures.DefaultMealId, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<string>.NotFound("Library meal not found"));

        var result = await _sut.RegenerateImage(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RegenerateImage_ServiceReturnsBadRequest_Returns400()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _mealService.Setup(s => s.RegenerateImageAsync(MealFixtures.DefaultMealId, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<string>.BadRequest("Image generation failed or is not configured"));

        var result = await _sut.RegenerateImage(MealFixtures.DefaultMealId, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
