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
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var user = UserFixtures.Member(); // PasswordHash = BCrypt("1234")
        _users.Setup(r => r.GetByUsernameAsync("alice", default)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("alice", "wrong"));

        result.Status.Should().Be(ServiceResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Login_WrongPassword_DoesNotSetAuthCookie()
    {
        var user = UserFixtures.Member();
        _users.Setup(r => r.GetByUsernameAsync("alice", default)).ReturnsAsync(user);

        await _sut.LoginAsync(new LoginRequest("alice", "wrong"));

        _currentUser.Verify(s => s.SetUserId(It.IsAny<Guid>()), Times.Never);
    }

    // --- ChangePassword ---

    [Fact]
    public async Task ChangePassword_ValidRequest_UpdatesPasswordHash()
    {
        var user = UserFixtures.Member();
        _users.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("1234", "NewPass1!extra"));

        result.IsSuccess.Should().BeTrue();
        _users.Verify(r => r.UpdatePasswordHashAsync(user.Id, It.Is<string>(h => BCrypt.Net.BCrypt.Verify("NewPass1!extra", h)), default), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_ReturnsUnauthorized()
    {
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.ChangePasswordAsync(UserFixtures.OutsideHouseholdId, new ChangePasswordRequest("1234", "NewPass1!extra"));

        result.Status.Should().Be(ServiceResultStatus.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsUnauthorized()
    {
        var user = UserFixtures.Member();
        _users.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("wrong", "NewPass1!extra"));

        result.Status.Should().Be(ServiceResultStatus.Unauthorized);
        _users.Verify(r => r.UpdatePasswordHashAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("nouppercase1!extra")]
    [InlineData("NOLOWERCASE1!EXTRA")]
    [InlineData("NoNumbers!extrabits")]
    [InlineData("NoSpecialChar1extra")]
    [InlineData("")]
    public async Task ChangePassword_InvalidNewPassword_ReturnsBadRequest(string newPassword)
    {
        var user = UserFixtures.Member();
        _users.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _sut.ChangePasswordAsync(user.Id, new ChangePasswordRequest("1234", newPassword));

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
        _users.Verify(r => r.UpdatePasswordHashAsync(It.IsAny<Guid>(), It.IsAny<string>(), default), Times.Never);
    }
}
