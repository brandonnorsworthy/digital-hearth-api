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

    private static readonly Guid TaskId5 = new("00000000-0000-0000-0000-000000000005");
    private static readonly Guid TaskId3 = new("00000000-0000-0000-0000-000000000003");
    private static readonly Guid TaskId7 = new("00000000-0000-0000-0000-000000000007");
    private static readonly Guid SubId99 = new("00000000-0000-0000-0000-000000000099");

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
        _repo.Setup(r => r.GetSubscriptionAsync(UserFixtures.DefaultId, It.IsAny<string>(), default))
            .ReturnsAsync((PushSubscription?)null);
        var user = UserFixtures.Member();

        var result = await _sut.SubscribeAsync(FakeSubRequest(), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeleteSubscriptionAsync(It.IsAny<PushSubscription>(), default), Times.Never);
        _repo.Verify(r => r.AddSubscriptionAsync(
            It.Is<PushSubscription>(s => s.UserId == UserFixtures.DefaultId && s.Endpoint == "https://push.example.com/endpoint"), default),
            Times.Once);
    }

    [Fact]
    public async Task Subscribe_ExistingEndpoint_DeletesThenAdds()
    {
        var existing = new PushSubscription { Id = SubId99, UserId = UserFixtures.DefaultId, Endpoint = "https://push.example.com/endpoint" };
        _repo.Setup(r => r.GetSubscriptionAsync(UserFixtures.DefaultId, "https://push.example.com/endpoint", default))
            .ReturnsAsync(existing);
        var user = UserFixtures.Member();

        var result = await _sut.SubscribeAsync(FakeSubRequest(), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeleteSubscriptionAsync(existing, default), Times.Once);
        _repo.Verify(r => r.AddSubscriptionAsync(It.IsAny<PushSubscription>(), default), Times.Once);
    }

    [Fact]
    public async Task Subscribe_StoresCorrectKeys()
    {
        _repo.Setup(r => r.GetSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<string>(), default))
            .ReturnsAsync((PushSubscription?)null);
        var user = UserFixtures.Member();

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
            new() { Id = TaskFixtures.DefaultId, UserId = UserFixtures.DefaultId },
            new() { Id = TaskFixtures.DefaultId2, UserId = UserFixtures.DefaultId }
        };
        _repo.Setup(r => r.GetSubscriptionsByUserAsync(UserFixtures.DefaultId, default)).ReturnsAsync(subs);
        var user = UserFixtures.Member();

        var result = await _sut.UnsubscribeAsync(user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeleteSubscriptionsAsync(subs, default), Times.Once);
    }

    [Fact]
    public async Task Unsubscribe_NoSubscriptions_ReturnsOkWithoutError()
    {
        _repo.Setup(r => r.GetSubscriptionsByUserAsync(UserFixtures.DefaultId, default)).ReturnsAsync([]);
        var user = UserFixtures.Member();

        var result = await _sut.UnsubscribeAsync(user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
    }

    // --- GetPreferences ---

    [Fact]
    public async Task GetPreferences_UserInHousehold_ReturnsOptedOutIds()
    {
        _repo.Setup(r => r.GetOptedOutTaskIdsAsync(UserFixtures.DefaultId, default)).ReturnsAsync([TaskId3, TaskId7]);
        var user = UserFixtures.InHousehold(UserFixtures.DefaultHouseholdId);

        var result = await _sut.GetPreferencesAsync(UserFixtures.DefaultHouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.OptedOutTaskIds.Should().BeEquivalentTo([TaskId3, TaskId7]);
    }

    [Fact]
    public async Task GetPreferences_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.GetPreferencesAsync(UserFixtures.DefaultHouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- OptOut ---

    [Fact]
    public async Task OptOut_NotAlreadyOptedOut_CreatesPreference()
    {
        _repo.Setup(r => r.IsOptedOutAsync(UserFixtures.DefaultId, TaskId5, default)).ReturnsAsync(false);
        var user = UserFixtures.Member();

        var result = await _sut.OptOutAsync(new OptOutRequest(TaskId5), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.AddPreferenceAsync(
            It.Is<NotifPreference>(p => p.UserId == UserFixtures.DefaultId && p.TaskId == TaskId5), default), Times.Once);
    }

    [Fact]
    public async Task OptOut_AlreadyOptedOut_IsIdempotentAndDoesNotDuplicate()
    {
        _repo.Setup(r => r.IsOptedOutAsync(UserFixtures.DefaultId, TaskId5, default)).ReturnsAsync(true);
        var user = UserFixtures.Member();

        var result = await _sut.OptOutAsync(new OptOutRequest(TaskId5), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.AddPreferenceAsync(It.IsAny<NotifPreference>(), default), Times.Never);
    }

    // --- RemoveOptOut ---

    [Fact]
    public async Task RemoveOptOut_PreferenceExists_DeletesIt()
    {
        var pref = new NotifPreference { Id = TaskFixtures.DefaultId, UserId = UserFixtures.DefaultId, TaskId = TaskId5 };
        _repo.Setup(r => r.GetPreferenceAsync(UserFixtures.DefaultId, TaskId5, default)).ReturnsAsync(pref);
        var user = UserFixtures.Member();

        var result = await _sut.RemoveOptOutAsync(TaskId5, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeletePreferenceAsync(pref, default), Times.Once);
    }

    [Fact]
    public async Task RemoveOptOut_PreferenceNotFound_ReturnsOkWithoutError()
    {
        _repo.Setup(r => r.GetPreferenceAsync(UserFixtures.DefaultId, TaskId5, default)).ReturnsAsync((NotifPreference?)null);
        var user = UserFixtures.Member();

        var result = await _sut.RemoveOptOutAsync(TaskId5, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _repo.Verify(r => r.DeletePreferenceAsync(It.IsAny<NotifPreference>(), default), Times.Never);
    }
}
