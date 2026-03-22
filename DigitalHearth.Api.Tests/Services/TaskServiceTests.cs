using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Models;
using DigitalHearth.Api.Repositories;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Moq;

namespace DigitalHearth.Api.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _tasks = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_tasks.Object);
    }

    // --- List ---

    [Fact]
    public async Task List_UserInHousehold_ReturnsOk()
    {
        _tasks.Setup(r => r.GetByHouseholdAsync(10, default)).ReturnsAsync([]);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.ListAsync(10, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
    }

    [Fact]
    public async Task List_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.ListAsync(10, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Fact]
    public async Task List_ReturnsTasksOrderedByNextDueAt()
    {
        var now = DateTime.UtcNow;
        var soon = new RecurringTask { Id = 1, HouseholdId = 10, Name = "A", IntervalDays = 1, CreatedAt = now };
        var later = new RecurringTask { Id = 2, HouseholdId = 10, Name = "B", IntervalDays = 30, CreatedAt = now };
        var middle = new RecurringTask { Id = 3, HouseholdId = 10, Name = "C", IntervalDays = 7, CreatedAt = now };
        _tasks.Setup(r => r.GetByHouseholdAsync(10, default)).ReturnsAsync([later, soon, middle]);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.ListAsync(10, user);

        var ids = result.Value!.Select(t => t.Id).ToList();
        ids.Should().Equal(1, 3, 2);
    }

    // --- Create ---

    [Fact]
    public async Task Create_ValidRequest_ReturnsOkWithTask()
    {
        var user = UserFixtures.InHousehold(10);
        _tasks.Setup(r => r.CreateAsync(It.IsAny<RecurringTask>(), default))
            .ReturnsAsync((RecurringTask t, CancellationToken _) => t);

        var result = await _sut.CreateAsync(10, new CreateTaskRequest("Vacuum", 7), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("Vacuum");
        result.Value.IntervalDays.Should().Be(7);
    }

    [Fact]
    public async Task Create_UserInDifferentHousehold_ReturnsForbidden()
    {
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.CreateAsync(10, new CreateTaskRequest("Vacuum", 7), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Create_InvalidIntervalDays_ReturnsBadRequest(int intervalDays)
    {
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.CreateAsync(10, new CreateTaskRequest("Vacuum", intervalDays), user);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    // --- Update ---

    [Fact]
    public async Task Update_ValidRequest_ReturnsOk()
    {
        var task = TaskFixtures.NeverCompleted();
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.UpdateAsync(1, new UpdateTaskRequest("New Name", 14), user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.Name.Should().Be("New Name");
        result.Value.IntervalDays.Should().Be(14);
    }

    [Fact]
    public async Task Update_TaskNotFound_ReturnsNotFound()
    {
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync((RecurringTask?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.UpdateAsync(1, new UpdateTaskRequest("New Name", null), user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task Update_TaskInDifferentHousehold_ReturnsForbidden()
    {
        var task = TaskFixtures.NeverCompleted(householdId: 10);
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.UpdateAsync(1, new UpdateTaskRequest("New Name", null), user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Update_InvalidIntervalDays_ReturnsBadRequest(int intervalDays)
    {
        var task = TaskFixtures.NeverCompleted();
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.UpdateAsync(1, new UpdateTaskRequest(null, intervalDays), user);

        result.Status.Should().Be(ServiceResultStatus.BadRequest);
    }

    [Fact]
    public async Task Update_PartialName_OnlyChangesName()
    {
        var task = TaskFixtures.NeverCompleted(intervalDays: 7);
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.UpdateAsync(1, new UpdateTaskRequest("New Name", null), user);

        result.Value!.Name.Should().Be("New Name");
        result.Value.IntervalDays.Should().Be(7);
    }

    [Fact]
    public async Task Update_PartialInterval_OnlyChangesInterval()
    {
        var task = TaskFixtures.NeverCompleted(intervalDays: 7);
        task.Name = "Original";
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.UpdateAsync(1, new UpdateTaskRequest(null, 14), user);

        result.Value!.Name.Should().Be("Original");
        result.Value.IntervalDays.Should().Be(14);
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_TaskFound_ReturnsOk()
    {
        var task = TaskFixtures.NeverCompleted();
        _tasks.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.DeleteAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        _tasks.Verify(r => r.DeleteAsync(task, default), Times.Once);
    }

    [Fact]
    public async Task Delete_TaskNotFound_ReturnsNotFound()
    {
        _tasks.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync((RecurringTask?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.DeleteAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task Delete_TaskInDifferentHousehold_ReturnsForbidden()
    {
        var task = TaskFixtures.NeverCompleted(householdId: 10);
        _tasks.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.DeleteAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- Complete ---

    [Fact]
    public async Task Complete_ValidTask_SetsLastCompletedAt()
    {
        var task = TaskFixtures.NeverCompleted();
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        _tasks.Setup(r => r.AddCompletionAsync(It.IsAny<TaskCompletion>(), default))
            .ReturnsAsync((TaskCompletion c, CancellationToken _) => c);
        var user = UserFixtures.InHousehold(10);
        var before = DateTime.UtcNow;

        var result = await _sut.CompleteAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
        result.Value!.LastCompletedAt.Should().NotBeNull();
        result.Value.LastCompletedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Complete_ValidTask_SetsLastCompletedByUsername()
    {
        var task = TaskFixtures.NeverCompleted();
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        _tasks.Setup(r => r.AddCompletionAsync(It.IsAny<TaskCompletion>(), default))
            .ReturnsAsync((TaskCompletion c, CancellationToken _) => c);
        var user = UserFixtures.Member(id: 1, username: "alice");

        var result = await _sut.CompleteAsync(1, user);

        result.Value!.LastCompletedBy.Should().Be("alice");
    }

    [Fact]
    public async Task Complete_ValidTask_CreatesCompletionRecord()
    {
        var task = TaskFixtures.NeverCompleted();
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        _tasks.Setup(r => r.AddCompletionAsync(It.IsAny<TaskCompletion>(), default))
            .ReturnsAsync((TaskCompletion c, CancellationToken _) => c);
        var user = UserFixtures.InHousehold(10);

        await _sut.CompleteAsync(1, user);

        _tasks.Verify(r => r.AddCompletionAsync(
            It.Is<TaskCompletion>(c => c.TaskId == 1 && c.UserId == user.Id), default), Times.Once);
    }

    [Fact]
    public async Task Complete_TaskNotFound_ReturnsNotFound()
    {
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync((RecurringTask?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.CompleteAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task Complete_TaskInDifferentHousehold_ReturnsForbidden()
    {
        var task = TaskFixtures.NeverCompleted(householdId: 10);
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.CompleteAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }

    // --- NextDueAt calculation ---

    [Fact]
    public async Task Create_NeverCompleted_NextDueAtIsCreatedAtPlusInterval()
    {
        var user = UserFixtures.InHousehold(10);
        RecurringTask? captured = null;
        _tasks.Setup(r => r.CreateAsync(It.IsAny<RecurringTask>(), default))
            .Callback<RecurringTask, CancellationToken>((t, _) => captured = t)
            .ReturnsAsync((RecurringTask t, CancellationToken _) => t);

        var result = await _sut.CreateAsync(10, new CreateTaskRequest("Vacuum", 7), user);

        result.Value!.NextDueAt.Should().Be(captured!.CreatedAt.AddDays(7));
    }

    [Fact]
    public async Task Complete_UpdatesNextDueAtToLastCompletedAtPlusInterval()
    {
        var task = TaskFixtures.NeverCompleted(intervalDays: 7);
        _tasks.Setup(r => r.GetByIdWithUserAsync(1, default)).ReturnsAsync(task);
        _tasks.Setup(r => r.AddCompletionAsync(It.IsAny<TaskCompletion>(), default))
            .ReturnsAsync((TaskCompletion c, CancellationToken _) => c);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.CompleteAsync(1, user);

        result.Value!.NextDueAt.Should().Be(result.Value.LastCompletedAt!.Value.AddDays(7));
    }

    // --- GetHistory ---

    [Fact]
    public async Task GetHistory_TaskFound_ReturnsOk()
    {
        var task = TaskFixtures.NeverCompleted();
        _tasks.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(task);
        _tasks.Setup(r => r.GetHistoryAsync(1, default)).ReturnsAsync([]);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.GetHistoryAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Ok);
    }

    [Fact]
    public async Task GetHistory_TaskNotFound_ReturnsNotFound()
    {
        _tasks.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync((RecurringTask?)null);
        var user = UserFixtures.InHousehold(10);

        var result = await _sut.GetHistoryAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.NotFound);
    }

    [Fact]
    public async Task GetHistory_TaskInDifferentHousehold_ReturnsForbidden()
    {
        var task = TaskFixtures.NeverCompleted(householdId: 10);
        _tasks.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(task);
        var user = UserFixtures.OutsideHousehold(99);

        var result = await _sut.GetHistoryAsync(1, user);

        result.Status.Should().Be(ServiceResultStatus.Forbidden);
    }
}
