using DigitalHearth.Api.Controllers;
using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DigitalHearth.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_currentUser.Object, _authService.Object);
        _sut.ControllerContext = new ControllerContext();
    }

    // --- Login ---

    [Fact]
    public async Task Login_ServiceReturnsOk_Returns200WithBody()
    {
        var me = new MeResponse(UserFixtures.DefaultId, "alice", UserFixtures.DefaultHouseholdId);
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), default))
            .ReturnsAsync(ServiceResult<MeResponse>.Ok(me));

        var result = await _sut.Login(new LoginRequest("alice", "1234"), default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(me);
    }

    [Fact]
    public async Task Login_ServiceReturnsUnauthorized_Returns401()
    {
        _authService
            .Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), default))
            .ReturnsAsync(ServiceResult<MeResponse>.Unauthorized("Invalid credentials"));

        var result = await _sut.Login(new LoginRequest("alice", "wrong"), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- Logout ---

    [Fact]
    public void Logout_ClearsCurrentUser_Returns204()
    {
        var result = _sut.Logout();

        _currentUser.Verify(s => s.Clear(), Times.Once);
        result.Should().BeOfType<NoContentResult>();
    }

    // --- Me ---

    [Fact]
    public async Task Me_Authenticated_Returns200WithMeResponse()
    {
        var user = UserFixtures.Member();
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(user);

        var result = await _sut.Me(default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var me = ok.Value.Should().BeOfType<MeResponse>().Subject;
        me.Id.Should().Be(user.Id);
        me.Username.Should().Be(user.Username);
        me.HouseholdId.Should().Be(user.HouseholdId);
    }

    [Fact]
    public async Task Me_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.Me(default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- ChangePin ---

    [Fact]
    public async Task ChangePin_Authenticated_ServiceReturnsOk_Returns204()
    {
        var user = UserFixtures.Member();
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(user);
        _authService
            .Setup(s => s.ChangePinAsync(user.Id, It.IsAny<ChangePinRequest>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.ChangePin(new ChangePinRequest("1234", "5678"), default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ChangePin_Authenticated_ServiceReturnsUnauthorized_Returns401()
    {
        var user = UserFixtures.Member();
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(user);
        _authService
            .Setup(s => s.ChangePinAsync(user.Id, It.IsAny<ChangePinRequest>(), default))
            .ReturnsAsync(ServiceResult.Unauthorized("Current PIN is incorrect"));

        var result = await _sut.ChangePin(new ChangePinRequest("wrong", "5678"), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ChangePin_Authenticated_ServiceReturnsBadRequest_Returns400()
    {
        var user = UserFixtures.Member();
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(user);
        _authService
            .Setup(s => s.ChangePinAsync(user.Id, It.IsAny<ChangePinRequest>(), default))
            .ReturnsAsync(ServiceResult.BadRequest("New PIN must be exactly 4 digits"));

        var result = await _sut.ChangePin(new ChangePinRequest("1234", "abc"), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePin_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.ChangePin(new ChangePinRequest("1234", "5678"), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
