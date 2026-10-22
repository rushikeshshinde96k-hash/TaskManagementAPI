using System.Net;
using System.Net.Http.Json;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.DTOs.Comments;
using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Notifications;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Integration;

public class TaskWorkflowApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public TaskWorkflowApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task User_CannotCreateTasks()
    {
        var member = await _factory.CreateClientAsAsync("member@test.com");

        var response = await member.PostAsJsonAsync("/api/tasks", new { title = "Sneaky task" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CannotCreateTeams()
    {
        var manager = await _factory.CreateClientAsAsync("manager@test.com");

        var response = await manager.PostAsJsonAsync("/api/teams", new { name = "Shadow team" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_CannotAssignOutsideTheirTeam()
    {
        var manager = await _factory.CreateClientAsAsync("manager@test.com");

        var response = await manager.PostAsJsonAsync("/api/tasks", new
        {
            title = "Not my team", teamId = ApiFactory.TeamId, assignedToId = ApiFactory.OutsiderId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullWorkflow_AssignUpdateStatusCommentAndNotify()
    {
        var manager = await _factory.CreateClientAsAsync("manager@test.com");
        var member = await _factory.CreateClientAsAsync("member@test.com");
        var outsider = await _factory.CreateClientAsAsync("outsider@test.com");

        // Manager creates and assigns a task to a team member.
        var create = await manager.PostAsJsonAsync("/api/tasks", new
        {
            title = "Workflow task",
            priority = "High",
            teamId = ApiFactory.TeamId,
            assignedToId = ApiFactory.MemberId,
            dueDate = DateTime.UtcNow.Date.AddDays(3)
        }, ApiFactory.Json);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var task = (await create.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.Json))!;
        Assert.Equal(TaskItemStatus.ToDo, task.Status);
        Assert.Equal("Member", task.AssignedToName);

        // Assignee is notified and sees the task; an unrelated user cannot.
        var memberNotes = await member.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", ApiFactory.Json);
        Assert.Contains(memberNotes!, n => n.Type == NotificationType.TaskAssigned && n.TaskId == task.Id);

        var memberTasks = await member.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks?pageSize=100", ApiFactory.Json);
        Assert.Contains(memberTasks!.Items, t => t.Id == task.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/api/tasks/{task.Id}")).StatusCode);

        // Assignee moves it to In Progress; manager is notified.
        var status = await member.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new { status = "InProgress" });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        var updated = await status.Content.ReadFromJsonAsync<TaskDto>(ApiFactory.Json);
        Assert.Equal(TaskItemStatus.InProgress, updated!.Status);

        var managerNotes = await manager.GetFromJsonAsync<List<NotificationDto>>("/api/notifications", ApiFactory.Json);
        Assert.Contains(managerNotes!, n => n.Type == NotificationType.TaskStatusChanged && n.TaskId == task.Id);

        // Collaboration through comments.
        var comment = await member.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new { content = "Started on this." });
        Assert.Equal(HttpStatusCode.Created, comment.StatusCode);
        var comments = await manager.GetFromJsonAsync<List<CommentDto>>($"/api/tasks/{task.Id}/comments", ApiFactory.Json);
        Assert.Single(comments!);

        // Assignee cannot delete the task; the manager can.
        Assert.Equal(HttpStatusCode.Forbidden, (await member.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await manager.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);
    }

    [Fact]
    public async Task Filters_ByStatusAndPriority()
    {
        var admin = await _factory.CreateClientAsAsync("admin@test.com");
        await admin.PostAsJsonAsync("/api/tasks", new { title = "Critical filter task", priority = "Critical", assignedToId = ApiFactory.ManagerId });
        await admin.PostAsJsonAsync("/api/tasks", new { title = "Low filter task", priority = "Low", assignedToId = ApiFactory.ManagerId });

        var result = await admin.GetFromJsonAsync<PagedResult<TaskDto>>("/api/tasks?priority=Critical&status=ToDo&pageSize=100", ApiFactory.Json);

        Assert.NotEmpty(result!.Items);
        Assert.All(result.Items, t =>
        {
            Assert.Equal(TaskPriority.Critical, t.Priority);
            Assert.Equal(TaskItemStatus.ToDo, t.Status);
        });
    }

    [Fact]
    public async Task Dashboard_ReturnsPerUserOverview()
    {
        var admin = await _factory.CreateClientAsAsync("admin@test.com");
        await admin.PostAsJsonAsync("/api/tasks", new { title = "Dashboard task", assignedToId = ApiFactory.OtherManagerId });

        var summary = await admin.GetFromJsonAsync<DashboardSummaryDto>("/api/dashboard/summary", ApiFactory.Json);

        Assert.True(summary!.Total >= 1);
        Assert.Equal(summary.Total, summary.ToDo + summary.InProgress + summary.Done);
        Assert.Contains(summary.ByUser, u => u.UserId == ApiFactory.OtherManagerId && u.ToDo >= 1);
    }

    [Fact]
    public async Task Admin_CanSeeAllUsers_UserCannot()
    {
        var admin = await _factory.CreateClientAsAsync("admin@test.com");
        var member = await _factory.CreateClientAsAsync("member@test.com");

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/users")).StatusCode);
    }
}
