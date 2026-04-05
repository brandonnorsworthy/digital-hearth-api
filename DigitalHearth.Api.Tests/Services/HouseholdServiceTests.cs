using DigitalHearth.Api.DTOs.Household;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace DigitalHearth.Api.Tests.Services;

public class HouseholdServiceTests
{
    private readonly Mock<IHouseholdRepository> _households = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IJoinCodeService> _joinCode = new();
    private readonly HouseholdService _sut;

    public HouseholdServiceTests()
    {
        _sut = new HouseholdService(_households.Object, _users.Object, _currentUser.Object, _joinCode.Object);
        _joinCode.Setup(j => j.GenerateUniqueCodeAsync(_households.Object, default)).ReturnsAsync("XYZ789");
        _users.Setup(r => r.UsernameExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _households.Setup(r => r.CreateAsync(It.IsAny<Household>(), default))
            .ReturnsAsync((Household h, CancellationToken _) => h);
        _users.Setup(r => r.CreateAsync(It.IsAny<User>(), default))
            .ReturnsAsync((User u, CancellationToken _) => u);
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidRequest_ReturnsOkWithHouseholdAndUser()
    {
        var req = new CreateHouseholdRequest("Test House", "alice", "1234", "Monday");

        var result = await _sut.CreateAsync(req);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Household.Name.Should().Be("Test House");
        result.Value.User.Username.Should().Be("alice");
    }

    [Fact]
    public async Task Create_ValidRequest_DefaultsWeekResetDayToMonday()
    {
        var req = new CreateHouseholdRequest("Test House", "alice", "1234", null);

        var result = await _sut.CreateAsync(req);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Household.WeekResetDay.Should().Be("Monday");
    }

    [Fact]
    public async Task Create_ValidRequest_PreservesUsernameCasing()
    {
        var req = new CreateHouseholdRequest("Test House", "Alice", "1234", null);

        var result = await _sut.CreateAsync(req);

        result.Value!.User.Username.Should().Be("Alice");
    }

    [Fact]
    public async Task Create_ValidRequest_CreatesAdminUser()
    {
        var req = new CreateHouseholdRequest("Test House", "alice", "1234", null);

        await _sut.CreateAsync(req);

        _users.Verify(r => r.CreateAsync(
            It.Is<User>(u => u.Role == "admin"), default), Times.Once);
    }

    [Fact]
    public async Task Create_ValidRequest_HashesPin()
    {
        var req = new CreateHouseholdRequest("Test House", "alice", "1234", null);

        await _sut.CreateAsync(req);

        _users.Verify(r => r.CreateAsync(
            It.Is<User>(u => u.PinHash != "1234" && BCrypt.Net.BCrypt.Verify("1234", u.PinHash)), default), Times.Once);
    }

    [Fact]
    public async Task Create_ValidRequest_SetsAuthCookie()
    {
        var req = new CreateHouseholdRequest("Test House", "alice", "1234", null);

        await _sut.CreateAsync(req);

        _currentUser.Verify(s => s.SetUserId(It.IsAny<Guid>()), Times.Once);
    }

    [Theory]
    [InlineData("", "alice", "1234")]
    [InlineData("  ", "alice", "1234")]
    [InlineData("Test House", "", "1234")]
    [InlineData("Test House", "alice", "")]
    public async Task Create_MissingRequiredFields_ReturnsBadRequest(string name, string username, string pin)
    {
        var req = new CreateHouseholdRequest(name, username, pin, null);

        var result = await _sut.CreateAsync(req);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    [Fact]
    public async Task Create_InvalidWeekResetDay_ReturnsBadRequest()
    {
        var req = new CreateHouseholdRequest("Test House", "alice", "1234", "Caturday");

        var result = await _sut.CreateAsync(req);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    [Fact]
    public async Task Create_UsernameTaken_ReturnsConflict()
    {
        _users.Setup(r => r.UsernameExistsAsync("alice", default)).ReturnsAsync(true);
        var req = new CreateHouseholdRequest("Test House", "alice", "1234", null);

        var result = await _sut.CreateAsync(req);

        result.Status.Should().Be(ServiceResultStatus.Conflict);
    }

    // --- Join ---

    [Fact]
    public async Task Join_ValidCode_ReturnsOkWithMemberUser()
    {
        var household = HouseholdFixtures.Default();
        _households.Setup(r => r.GetByJoinCodeAsync("ABC123", default)).ReturnsAsync(household);
        var req = new JoinHouseholdRequest("bob", "5678", "ABC123");

        var result = await _sut.JoinAsync(req);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.User.Username.Should().Be("bob");
        result.Value.Household.Id.Should().Be(household.Id);
    }

    [Fact]
    public async Task Join_ValidCode_CreatesMemberRole()
    {
        _households.Setup(r => r.GetByJoinCodeAsync("ABC123", default)).ReturnsAsync(HouseholdFixtures.Default());
        var req = new JoinHouseholdRequest("bob", "5678", "ABC123");

        await _sut.JoinAsync(req);

        _users.Verify(r => r.CreateAsync(
            It.Is<User>(u => u.Role == "member"), default), Times.Once);
    }

    [Fact]
    public async Task Join_JoinCodeNotFound_ReturnsNotFound()
    {
        _households.Setup(r => r.GetByJoinCodeAsync(It.IsAny<string>(), default)).ReturnsAsync((Household?)null);
        var req = new JoinHouseholdRequest("bob", "5678", "NOPE99");

        var result = await _sut.JoinAsync(req);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task Join_UsernameTaken_ReturnsConflict()
    {
        _households.Setup(r => r.GetByJoinCodeAsync("ABC123", default)).ReturnsAsync(HouseholdFixtures.Default());
        _users.Setup(r => r.UsernameExistsAsync("bob", default)).ReturnsAsync(true);
        var req = new JoinHouseholdRequest("bob", "5678", "ABC123");

        var result = await _sut.JoinAsync(req);

        result.Status.Should().Be(ServiceResultStatus.Conflict);
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_UserInHousehold_ReturnsOk()
    {
        var household = HouseholdFixtures.Default();
        _households.Setup(r => r.GetByIdAsync(UserFixtures.DefaultHouseholdId, default)).ReturnsAsync(household);
        var user = UserFixtures.InHousehold(UserFixtures.DefaultHouseholdId);

        var result = await _sut.GetByIdAsync(UserFixtures.DefaultHouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Id.Should().Be(UserFixtures.DefaultHouseholdId);
    }

    [Fact]
    public async Task GetById_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.GetByIdAsync(UserFixtures.DefaultHouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task GetById_HouseholdNotFound_ReturnsNotFound()
    {
        _households.Setup(r => r.GetByIdAsync(UserFixtures.DefaultHouseholdId, default)).ReturnsAsync((Household?)null);
        var user = UserFixtures.InHousehold(UserFixtures.DefaultHouseholdId);

        var result = await _sut.GetByIdAsync(UserFixtures.DefaultHouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task GetById_MapsWeekResetDayIntToName()
    {
        var household = HouseholdFixtures.Default(weekResetDay: 3); // Wednesday
        _households.Setup(r => r.GetByIdAsync(UserFixtures.DefaultHouseholdId, default)).ReturnsAsync(household);
        var user = UserFixtures.InHousehold(UserFixtures.DefaultHouseholdId);

        var result = await _sut.GetByIdAsync(UserFixtures.DefaultHouseholdId, user);

        result.Value!.WeekResetDay.Should().Be("Wednesday");
    }

    // --- GetMembers ---

    [Fact]
    public async Task GetMembers_UserInHousehold_ReturnsOk()
    {
        _users.Setup(r => r.GetMembersByHouseholdAsync(UserFixtures.DefaultHouseholdId, default)).ReturnsAsync([]);
        var user = UserFixtures.InHousehold(UserFixtures.DefaultHouseholdId);

        var result = await _sut.GetMembersAsync(UserFixtures.DefaultHouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
    }

    [Fact]
    public async Task GetMembers_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold();

        var result = await _sut.GetMembersAsync(UserFixtures.DefaultHouseholdId, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- Update ---

    [Fact]
    public async Task Update_AdminUser_ReturnsOk()
    {
        var household = HouseholdFixtures.Default();
        _households.Setup(r => r.GetByIdAsync(UserFixtures.DefaultHouseholdId, default)).ReturnsAsync(household);
        var user = UserFixtures.Admin();

        var result = await _sut.UpdateAsync(UserFixtures.DefaultHouseholdId, new UpdateHouseholdRequest("New Name", null, null, null), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Update_MemberUser_ReturnsForbidden()
    {
        var user = UserFixtures.Member();

        var result = await _sut.UpdateAsync(UserFixtures.DefaultHouseholdId, new UpdateHouseholdRequest("New Name", null, null, null), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task Update_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.Admin(householdId: UserFixtures.OutsideHouseholdId);

        var result = await _sut.UpdateAsync(UserFixtures.DefaultHouseholdId, new UpdateHouseholdRequest("New Name", null, null, null), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task Update_InvalidWeekResetDay_ReturnsBadRequest()
    {
        var household = HouseholdFixtures.Default();
        _households.Setup(r => r.GetByIdAsync(UserFixtures.DefaultHouseholdId, default)).ReturnsAsync(household);
        var user = UserFixtures.Admin();

        var result = await _sut.UpdateAsync(UserFixtures.DefaultHouseholdId, new UpdateHouseholdRequest(null, "Caturday", null, null), user);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    [Fact]
    public async Task Update_HouseholdNotFound_ReturnsNotFound()
    {
        _households.Setup(r => r.GetByIdAsync(UserFixtures.DefaultHouseholdId, default)).ReturnsAsync((Household?)null);
        var user = UserFixtures.Admin();

        var result = await _sut.UpdateAsync(UserFixtures.DefaultHouseholdId, new UpdateHouseholdRequest("New Name", null, null, null), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task Update_PartialUpdate_OnlyChangesProvidedFields()
    {
        var household = HouseholdFixtures.Default(name: "Original", weekResetDay: 1);
        _households.Setup(r => r.GetByIdAsync(UserFixtures.DefaultHouseholdId, default)).ReturnsAsync(household);
        var user = UserFixtures.Admin();

        var result = await _sut.UpdateAsync(UserFixtures.DefaultHouseholdId, new UpdateHouseholdRequest(null, "Friday", null, null), user);

        result.Value!.Name.Should().Be("Original");
        result.Value.WeekResetDay.Should().Be("Friday");
    }
}
