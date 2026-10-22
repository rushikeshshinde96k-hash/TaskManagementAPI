using NSubstitute;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Unit;

public class TaskServiceTests : ServiceTestBase
{
    private readonly TaskService _sut;
    private readonly Team _team = new() { Id = 10, Name = "Engineering", ManagerId = Manager.Id, Manager = Manager };

    public TaskServiceTests()
    {
        _sut = new TaskService(Uow, Current, Notifier);
        Teams.GetByIdAsync(_team.Id, Arg.Any<CancellationToken>()).Returns(_team);
        Teams.IsMemberAsync(_team.Id, Member.Id, Arg.Any<CancellationToken>()).Returns(true);
        Tasks.GetDtoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(ci => new TaskDto { Id = ci.Arg<int>() });
    }

    [Fact]
    public async Task Create_AsUser_IsForbidden()
    {
        ActAs(Member);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.CreateAsync(new CreateTaskRequest { Title = "Task" }));
    }

    [Fact]
    public async Task Create_ManagerForTeamTheyDoNotManage_IsForbidden()
    {
        ActAs(OtherManager);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.CreateAsync(new CreateTaskRequest { Title = "Task", TeamId = _team.Id, AssignedToId = Member.Id }));
    }

    [Fact]
    public async Task Create_AssigneeNotInTeam_IsBadRequest()
    {
        ActAs(Manager);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreateAsync(new CreateTaskRequest { Title = "Task", TeamId = _team.Id, AssignedToId = Outsider.Id }));
    }

    [Fact]
    public async Task Create_WithPastDueDate_IsBadRequest()
    {
        ActAs(Manager);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreateAsync(new CreateTaskRequest { Title = "Task", DueDate = DateTime.UtcNow.AddDays(-2) }));
    }

    [Fact]
    public async Task Create_Valid_SavesTaskAndNotifiesAssignee()
    {
        ActAs(Manager);

        await _sut.CreateAsync(new CreateTaskRequest
        {
            Title = "Build login page",
            TeamId = _team.Id,
            AssignedToId = Member.Id,
            Priority = TaskPriority.High,
            DueDate = DateTime.UtcNow.AddDays(3)
        });

        await Tasks.Received(1).AddAsync(Arg.Is<TaskItem>(t =>
            t.Title == "Build login page" && t.CreatedById == Manager.Id && t.Status == TaskItemStatus.ToDo),
            Arg.Any<CancellationToken>());
        await Uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        await Notifier.Received(1).NotifyTaskAssignedAsync(Arg.Any<TaskItem>(), Member, Manager, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_AdminAssigningToAnotherAdmin_IsBadRequest()
    {
        var otherAdmin = new User { Id = 99, FullName = "Admin 2", Role = UserRole.Admin, IsActive = true };
        Users.GetByIdAsync(otherAdmin.Id, Arg.Any<CancellationToken>()).Returns(otherAdmin);
        ActAs(Admin);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreateAsync(new CreateTaskRequest { Title = "Task", AssignedToId = otherAdmin.Id }));
    }

    [Fact]
    public async Task UpdateStatus_ByAssignee_MarksDoneAndNotifies()
    {
        var task = NewTask(assignedTo: Member);
        Tasks.GetWithDetailsAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        ActAs(Member);

        await _sut.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = TaskItemStatus.Done });

        Assert.Equal(TaskItemStatus.Done, task.Status);
        Assert.NotNull(task.CompletedAt);
        await Notifier.Received(1).NotifyTaskStatusChangedAsync(task, TaskItemStatus.ToDo, Member, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatus_ByUnrelatedUser_IsForbidden()
    {
        var task = NewTask(assignedTo: Member);
        Tasks.GetWithDetailsAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        ActAs(Outsider);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = TaskItemStatus.InProgress }));
        await Notifier.DidNotReceiveWithAnyArgs().NotifyTaskStatusChangedAsync(default!, default, default!);
    }

    [Fact]
    public async Task UpdateStatus_SameStatus_DoesNotNotify()
    {
        var task = NewTask(assignedTo: Member);
        Tasks.GetWithDetailsAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        ActAs(Member);

        await _sut.UpdateStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = TaskItemStatus.ToDo });

        await Notifier.DidNotReceiveWithAnyArgs().NotifyTaskStatusChangedAsync(default!, default, default!);
    }

    [Fact]
    public async Task Delete_ByAssignedUser_IsForbidden()
    {
        var task = NewTask(assignedTo: Member);
        Tasks.GetWithDetailsAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);
        ActAs(Member);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeleteAsync(task.Id));
        Tasks.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task GetById_TaskNotFound_ThrowsNotFound()
    {
        ActAs(Admin);
        Tasks.GetWithDetailsAsync(123, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(123));
    }

    private TaskItem NewTask(User assignedTo) => new()
    {
        Id = 50,
        Title = "Existing task",
        Status = TaskItemStatus.ToDo,
        TeamId = _team.Id,
        Team = _team,
        AssignedToId = assignedTo.Id,
        AssignedTo = assignedTo,
        CreatedById = Manager.Id,
        CreatedBy = Manager
    };
}
