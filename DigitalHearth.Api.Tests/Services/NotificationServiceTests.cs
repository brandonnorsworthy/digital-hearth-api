using DigitalHearth.Api.DTOs.Notification;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace DigitalHearth.Api.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _repo = new();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(_repo.Object);
    }

    private static PushSubscriptionRequest FakeSubRequest() =>
        new("https://push.example.com/endpoint", "p256dh-key", "auth-secret");

    // --- Subscribe ---

    [Fact]
    public async Task Subscribe_NewEndpoint_AddsSubscriptionWithoutDeleting()
    {
        _repo.Setup(r => r.GetSubscriptionAsync(1, It.IsAny<string>(), default))
            .ReturnsAsync((PushSubscription?)null);
        var user = UserFixtures.Member(id: 1);

        var result = await _sut.SubscribeAsync(FakeSubRequest(), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeleteSubscriptionAsync(It.IsAny<PushSubscription>(), default), Times.Never);
        _repo.Verify(r => r.AddSubscriptionAsync(
            It.Is<PushSubscription>(s => s.UserId == 1 && s.Endpoint == "https://push.example.com/endpoint"), default),
            Times.Once);
    }

    [Fact]
    public async Task Subscribe_ExistingEndpoint_DeletesThenAdds()
    {
        var existing = new PushSubscription { Id = 99, UserId = 1, Endpoint = "https://push.example.com/endpoint" };
        _repo.Setup(r => r.GetSubscriptionAsync(1, "https://push.example.com/endpoint", default))
            .ReturnsAsync(existing);
        var user = UserFixtures.Member(id: 1);

        var result = await _sut.SubscribeAsync(FakeSubRequest(), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeleteSubscriptionAsync(existing, default), Times.Once);
        _repo.Verify(r => r.AddSubscriptionAsync(It.IsAny<PushSubscription>(), default), Times.Once);
    }

    [Fact]
    public async Task Subscribe_StoresCorrectKeys()
    {
        _repo.Setup(r => r.GetSubscriptionAsync(It.IsAny<int>(), It.IsAny<string>(), default))
            .ReturnsAsync((PushSubscription?)null);
        var user = UserFixtures.Member(id: 1);

        await _sut.SubscribeAsync(FakeSubRequest(), user);

        _repo.Verify(r => r.AddSubscriptionAsync(
            It.Is<PushSubscription>(s => s.P256dh == "p256dh-key" && s.Auth == "auth-secret"), default),
            Times.Once);
    }

    // --- Unsubscribe ---

    [Fact]
    public async Task Unsubscribe_HasSubscriptions_DeletesAll()
    {
        var subs = new List<PushSubscription>
        {
            new() { Id = 1, UserId = 1 },
            new() { Id = 2, UserId = 1 }
        };
        _repo.Setup(r => r.GetSubscriptionsByUserAsync(1, default)).ReturnsAsync(subs);
        var user = UserFixtures.Member(id: 1);

        var result = await _sut.UnsubscribeAsync(user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeleteSubscriptionsAsync(subs, default), Times.Once);
    }

    [Fact]
    public async Task Unsubscribe_NoSubscriptions_ReturnsOkWithoutError()
    {
        _repo.Setup(r => r.GetSubscriptionsByUserAsync(1, default)).ReturnsAsync([]);
        var user = UserFixtures.Member(id: 1);

        var result = await _sut.UnsubscribeAsync(user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
    }

    // --- GetPreferences ---

    [Fact]
    public async Task GetPreferences_UserInHousehold_ReturnsOptedOutIds()
    {
        _repo.Setup(r => r.GetOptedOutTaskIdsAsync(1, default)).ReturnsAsync([3, 7]);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.GetPreferencesAsync(10, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.OptedOutTaskIds.Should().BeEquivalentTo([3, 7]);
    }

    [Fact]
    public async Task GetPreferences_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.GetPreferencesAsync(10, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- OptOut ---

    [Fact]
    public async Task OptOut_NotAlreadyOptedOut_CreatesPreference()
    {
        _repo.Setup(r => r.IsOptedOutAsync(1, 5, default)).ReturnsAsync(false);
        var user = UserFixtures.Member(id: 1);

        var result = await _sut.OptOutAsync(new OptOutRequest(5), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.AddPreferenceAsync(
            It.Is<NotifPreference>(p => p.UserId == 1 && p.TaskId == 5), default), Times.Once);
    }

    [Fact]
    public async Task OptOut_AlreadyOptedOut_IsIdempotentAndDoesNotDuplicate()
    {
        _repo.Setup(r => r.IsOptedOutAsync(1, 5, default)).ReturnsAsync(true);
        var user = UserFixtures.Member(id: 1);

        var result = await _sut.OptOutAsync(new OptOutRequest(5), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.AddPreferenceAsync(It.IsAny<NotifPreference>(), default), Times.Never);
    }

    // --- RemoveOptOut ---

    [Fact]
    public async Task RemoveOptOut_PreferenceExists_DeletesIt()
    {
        var pref = new NotifPreference { Id = 1, UserId = 1, TaskId = 5 };
        _repo.Setup(r => r.GetPreferenceAsync(1, 5, default)).ReturnsAsync(pref);
        var user = UserFixtures.Member(id: 1);

        var result = await _sut.RemoveOptOutAsync(5, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeletePreferenceAsync(pref, default), Times.Once);
    }

    [Fact]
    public async Task RemoveOptOut_PreferenceNotFound_ReturnsOkWithoutError()
    {
        _repo.Setup(r => r.GetPreferenceAsync(1, 5, default)).ReturnsAsync((NotifPreference?)null);
        var user = UserFixtures.Member(id: 1);

        var result = await _sut.RemoveOptOutAsync(5, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeletePreferenceAsync(It.IsAny<NotifPreference>(), default), Times.Never);
    }
}
