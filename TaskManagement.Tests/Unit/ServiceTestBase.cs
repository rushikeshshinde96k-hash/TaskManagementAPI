using NSubstitute;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Interfaces.Repositories;
using TaskManagement.Application.Services.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Unit;

/// <summary>Substitutes for the unit of work, its repositories and the current user.</summary>
public abstract class ServiceTestBase
{
    protected readonly IUnitOfWork Uow = Substitute.For<IUnitOfWork>();
    protected readonly IUserRepository Users = Substitute.For<IUserRepository>();
    protected readonly ITeamRepository Teams = Substitute.For<ITeamRepository>();
    protected readonly ITaskRepository Tasks = Substitute.For<ITaskRepository>();
    protected readonly ICommentRepository Comments = Substitute.For<ICommentRepository>();
    protected readonly INotificationRepository Notifications = Substitute.For<INotificationRepository>();
    protected readonly IRefreshTokenRepository RefreshTokens = Substitute.For<IRefreshTokenRepository>();
    protected readonly ICurrentUserService Current = Substitute.For<ICurrentUserService>();
    protected readonly INotificationService Notifier = Substitute.For<INotificationService>();

    protected static readonly User Admin = new() { Id = 1, FullName = "Admin", Email = "admin@t.com", Role = UserRole.Admin };
    protected static readonly User Manager = new() { Id = 2, FullName = "Manager", Email = "manager@t.com", Role = UserRole.Manager };
    protected static readonly User OtherManager = new() { Id = 3, FullName = "Other Manager", Email = "other@t.com", Role = UserRole.Manager };
    protected static readonly User Member = new() { Id = 4, FullName = "Member", Email = "member@t.com", Role = UserRole.User };
    protected static readonly User Outsider = new() { Id = 5, FullName = "Outsider", Email = "outsider@t.com", Role = UserRole.User };

    protected ServiceTestBase()
    {
        Uow.Users.Returns(Users);
        Uow.Teams.Returns(Teams);
        Uow.Tasks.Returns(Tasks);
        Uow.Comments.Returns(Comments);
        Uow.Notifications.Returns(Notifications);
        Uow.RefreshTokens.Returns(RefreshTokens);

        foreach (var u in new[] { Admin, Manager, OtherManager, Member, Outsider })
            Users.GetByIdAsync(u.Id, Arg.Any<CancellationToken>()).Returns(u);
    }

    protected void ActAs(User user)
    {
        Current.UserId.Returns(user.Id);
        Current.Role.Returns(user.Role);
        Current.Scope.Returns(new AccessScope(user.Id, user.Role));
        Current.IsAuthenticated.Returns(true);
    }
}
