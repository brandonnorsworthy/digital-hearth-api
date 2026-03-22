using DigitalHearth.Api.Controllers;
using DigitalHearth.Api.DTOs.Task;
using DigitalHearth.Api.Services;
using DigitalHearth.Api.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DigitalHearth.Api.Tests.Controllers;

public class TaskControllerTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<ITaskService> _taskService = new();
    private readonly TaskController _sut;

    private static readonly TaskResponse FakeTask = new(1, 10, "Vacuum", 7, null, null, DateTime.UtcNow.AddDays(7));

    public TaskControllerTests()
    {
        _sut = new TaskController(_currentUser.Object, _taskService.Object);
        _sut.ControllerContext = new ControllerContext();
    }

    // --- List ---

    [Fact]
    public async Task List_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.ListAsync(10, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<IReadOnlyList<TaskResponse>>.Ok([FakeTask]));

        var result = await _sut.List(10, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task List_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.List(10, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task List_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.ListAsync(10, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<IReadOnlyList<TaskResponse>>.Forbidden());

        var result = await _sut.List(10, default);

        result.Should().BeOfType<ForbidResult>();
    }

    // --- Create ---

    [Fact]
    public async Task Create_ServiceReturnsOk_Returns201()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.CreateAsync(10, It.IsAny<CreateTaskRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<TaskResponse>.Ok(FakeTask));

        var result = await _sut.Create(10, new CreateTaskRequest("Vacuum", 7), default);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(FakeTask);
    }

    [Fact]
    public async Task Create_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.Create(10, new CreateTaskRequest("Vacuum", 7), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Create_ServiceReturnsBadRequest_Returns400()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.CreateAsync(10, It.IsAny<CreateTaskRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<TaskResponse>.BadRequest("IntervalDays must be greater than 0"));

        var result = await _sut.Create(10, new CreateTaskRequest("Vacuum", 0), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // --- Update ---

    [Fact]
    public async Task Update_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateTaskRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<TaskResponse>.Ok(FakeTask));

        var result = await _sut.Update(1, new UpdateTaskRequest("New Name", null), default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.Update(1, new UpdateTaskRequest("New Name", null), default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Update_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateTaskRequest>(), It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<TaskResponse>.Forbidden());

        var result = await _sut.Update(1, new UpdateTaskRequest("New Name", null), default);

        result.Should().BeOfType<ForbidResult>();
    }

    // --- Delete ---

    [Fact]
    public async Task Delete_Authenticated_ServiceReturnsOk_Returns204()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.DeleteAsync(1, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.Ok());

        var result = await _sut.Delete(1, default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.Delete(1, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Delete_ServiceReturnsNotFound_Returns404()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.DeleteAsync(1, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult.NotFound("Task not found"));

        var result = await _sut.Delete(1, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // --- Complete ---

    [Fact]
    public async Task Complete_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.CompleteAsync(1, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<TaskResponse>.Ok(FakeTask));

        var result = await _sut.Complete(1, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Complete_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.Complete(1, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Complete_ServiceReturnsForbidden_Returns403()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.CompleteAsync(1, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<TaskResponse>.Forbidden());

        var result = await _sut.Complete(1, default);

        result.Should().BeOfType<ForbidResult>();
    }

    // --- History ---

    [Fact]
    public async Task History_Authenticated_ServiceReturnsOk_Returns200()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.GetHistoryAsync(1, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<IReadOnlyList<CompletionResponse>>.Ok([]));

        var result = await _sut.History(1, default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task History_NotAuthenticated_Returns401()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync((Models.User?)null);

        var result = await _sut.History(1, default);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task History_ServiceReturnsNotFound_Returns404()
    {
        _currentUser.Setup(s => s.GetUserAsync(default)).ReturnsAsync(UserFixtures.Member());
        _taskService.Setup(s => s.GetHistoryAsync(1, It.IsAny<Models.User>(), default))
            .ReturnsAsync(ServiceResult<IReadOnlyList<CompletionResponse>>.NotFound("Task not found"));

        var result = await _sut.History(1, default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
