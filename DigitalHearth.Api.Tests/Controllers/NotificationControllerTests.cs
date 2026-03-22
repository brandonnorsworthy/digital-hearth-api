using DigitalHearth.Api.Controllers;
using DigitalHearth.Api.DTOs.Notification;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DigitalHearth.Api.Tests.Controllers;

public class NotificationControllerTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly Mock<IPushNotificationService> _push = new();
    private readonly Mock<IConfiguration> _config = new();
    private readonly NotificationController _sut;

    public NotificationControllerTests()
    {
        _sut = new NotificationController(_currentUser.Object, _notificationService.Object, _push.Object, _config.Object);
        _sut.ControllerContext = new ControllerContext();
    }

    private static PushSubscriptionRequest FakeSubRequest() =>
        new("https://push.example.com/endpoint", "p256dh-key", "auth-secret");

    // --- GetVapidPublicKey ---

    [Fact]
    public void GetVapidPublicKey_KeyConfigured_Returns200()
    {
        _config.Setup(c => c["Vapid:PublicKey"]).Returns("my-vapid-key");

        var result = _sut.GetVapidPublicKey();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void GetVapidPublicKey_KeyNotConfigured_Returns503()
    {
        _config.Setup(c => c["Vapid:PublicKey"]).Returns((string?)null);

        var result = _sut.GetVapidPublicKey();

        var status = result.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(503);
    }

    // --- Subscribe ---

    [Fact]
    public async Task Subscribe_Authenticated_Returns201()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _notificationService.Setup(s => s.SubscribeAsync(It.IsAny<PushSubscriptionRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.Subscribe(FakeSubRequest(), default);

        var status = result.Should().BeOfType<StatusCodeResult>().Subject;
        status.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Subscribe_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.Subscribe(FakeSubRequest(), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- Unsubscribe ---

    [Fact]
    public async Task Unsubscribe_Authenticated_Returns204()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _notificationService.Setup(s => s.UnsubscribeAsync(It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.Unsubscribe(default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Unsubscribe_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.Unsubscribe(default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- GetPreferences ---

    [Fact]
    public async Task GetPreferences_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _notificationService.Setup(s => s.GetPreferencesAsync(10, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<PreferencesResponse>.Ok(new PreferencesResponse([])));

        var result = await _sut.GetPreferences(10, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPreferences_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.GetPreferences(10, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetPreferences_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _notificationService.Setup(s => s.GetPreferencesAsync(10, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<PreferencesResponse>.Forbidden());

        var result = await _sut.GetPreferences(10, default);

        result.Should().BeOfType<ForbidResult>();
    }

    // --- OptOut ---

    [Fact]
    public async Task OptOut_Authenticated_Returns201()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _notificationService.Setup(s => s.OptOutAsync(It.IsAny<OptOutRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.OptOut(new OptOutRequest(5), default);

        var status = result.Should().BeOfType<StatusCodeResult>().Subject;
        status.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task OptOut_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.OptOut(new OptOutRequest(5), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // --- RemoveOptOut ---

    [Fact]
    public async Task RemoveOptOut_Authenticated_Returns204()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _notificationService.Setup(s => s.RemoveOptOutAsync(5, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.RemoveOptOut(5, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveOptOut_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.RemoveOptOut(5, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
