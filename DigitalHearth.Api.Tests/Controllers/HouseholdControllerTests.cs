using DigitalHearth.Api.Controllers;
using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DigitalHearth.Api.Tests.Controllers;

public class HouseholdControllerTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IHouseholdService> _householdService = new();
    private readonly HouseholdController _sut;

    private static readonly HouseholdResponse FakeHousehold = new(10, "Test House", "ABC123", "Monday", null);
    private static readonly MeResponse FakeMe = new(1, "alice", 10);
    private static readonly HouseholdWithUserResponse FakeCreated = new(FakeMe, FakeHousehold);

    public HouseholdControllerTests()
    {
        _sut = new HouseholdController(_currentUser.Object, _householdService.Object);
        _sut.ControllerContext = new ControllerContext();
    }

    // --- Create ---

    [Fact]
    public async Task Create_ServiceReturnsOk_Returns201CreatedAtAction()
    {
        _householdService
            .Setup(s => s.CreateAsync(It.IsAny<CreateHouseholdRequest>(), default))
            .ReturnsAsync(ServiceResult<HouseholdWithUserResponse>.Ok(FakeCreated));

        var result = await _sut.Create(new CreateHouseholdRequest("Test House", "alice", "1234", null), default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(FakeCreated);
        created.ActionName.Should().Be(nameof(_sut.GetById));
    }

    [Fact]
    public async Task Create_ServiceReturnsConflict_Returns409()
    {
        _householdService
            .Setup(s => s.CreateAsync(It.IsAny<CreateHouseholdRequest>(), default))
            .ReturnsAsync(ServiceResult<HouseholdWithUserResponse>.Conflict("Username already taken"));

        var result = await _sut.Create(new CreateHouseholdRequest("Test House", "alice", "1234", null), default);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_ServiceReturnsBadRequest_Returns400()
    {
        _householdService
            .Setup(s => s.CreateAsync(It.IsAny<CreateHouseholdRequest>(), default))
            .ReturnsAsync(ServiceResult<HouseholdWithUserResponse>.BadRequest("Invalid day"));

        var result = await _sut.Create(new CreateHouseholdRequest("Test House", "alice", "1234", "Caturday"), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // --- Join ---

    [Fact]
    public async Task Join_ServiceReturnsOk_Returns200()
    {
        _householdService
            .Setup(s => s.JoinAsync(It.IsAny<JoinHouseholdRequest>(), default))
            .ReturnsAsync(ServiceResult<HouseholdWithUserResponse>.Ok(FakeCreated));

        var result = await _sut.Join(new JoinHouseholdRequest("bob", "5678", "ABC123"), default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(FakeCreated);
    }

    [Fact]
    public async Task Join_ServiceReturnsNotFound_Returns404()
    {
        _householdService
            .Setup(s => s.JoinAsync(It.IsAny<JoinHouseholdRequest>(), default))
            .ReturnsAsync(ServiceResult<HouseholdWithUserResponse>.NotFound("Join code not found"));

        var result = await _sut.Join(new JoinHouseholdRequest("bob", "5678", "NOPE99"), default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _householdService
            .Setup(s => s.GetByIdAsync(10, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<HouseholdResponse>.Ok(FakeHousehold));

        var result = await _sut.GetById(10, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.GetById(10, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetById_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _householdService
            .Setup(s => s.GetByIdAsync(10, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<HouseholdResponse>.Forbidden());

        var result = await _sut.GetById(10, default);

        result.Should().BeOfType<ForbidResult>();
    }

    // --- GetMembers ---

    [Fact]
    public async Task GetMembers_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _householdService
            .Setup(s => s.GetMembersAsync(10, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<IReadOnlyList<MemberResponse>>.Ok([]));

        var result = await _sut.GetMembers(10, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMembers_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.GetMembers(10, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- Update ---

    [Fact]
    public async Task Update_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Admin());
        _householdService
            .Setup(s => s.UpdateAsync(10, It.IsAny<UpdateHouseholdRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<HouseholdResponse>.Ok(FakeHousehold));

        var result = await _sut.Update(10, new UpdateHouseholdRequest("New Name", null, null), default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.Update(10, new UpdateHouseholdRequest("New Name", null, null), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Update_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _householdService
            .Setup(s => s.UpdateAsync(10, It.IsAny<UpdateHouseholdRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<HouseholdResponse>.Forbidden());

        var result = await _sut.Update(10, new UpdateHouseholdRequest("New Name", null, null), default);

        result.Should().BeOfType<ForbidResult>();
    }
}
