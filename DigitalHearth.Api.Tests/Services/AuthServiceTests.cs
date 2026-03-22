using DigitalHearth.Api.DTOs.Auth;
using DigitalHearth.Api.Repositories;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace DigitalHearth.Api.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users.Object, _currentUser.Object);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithMeResponse()
    {
        var user = UserFixtures.Member();
        _users.Setup(r => r.GetByUsernameAsync("alice", default)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("alice", "1234"));

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Id.Should().Be(user.Id);
        result.Value.Username.Should().Be(user.Username);
        result.Value.HouseholdId.Should().Be(user.HouseholdId);
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsAuthCookie()
    {
        var user = UserFixtures.Member();
        _users.Setup(r => r.GetByUsernameAsync("alice", default)).ReturnsAsync(user);

        await _sut.LoginAsync(new LoginRequest("alice", "1234"));

        _currentUser.Verify(s => s.SetUserId(user.Id), Times.Once);
    }

    [Fact]
    public async Task Login_UserNotFound_ReturnsUnauthorized()
    {
        _users.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.LoginAsync(new LoginRequest("ghost", "1234"));

        result.Status.Should().Be(ServiceResultStatus.Unauthorized);
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPin_ReturnsUnauthorized()
    {
        var user = UserFixtures.Member(); // PinHash = BCrypt("1234")
        _users.Setup(r => r.GetByUsernameAsync("alice", default)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("alice", "wrong"));

        result.Status.Should().Be(ServiceResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Login_WrongPin_DoesNotSetAuthCookie()
    {
        var user = UserFixtures.Member();
        _users.Setup(r => r.GetByUsernameAsync("alice", default)).ReturnsAsync(user);

        await _sut.LoginAsync(new LoginRequest("alice", "wrong"));

        _currentUser.Verify(s => s.SetUserId(It.IsAny<int>()), Times.Never);
    }
}
