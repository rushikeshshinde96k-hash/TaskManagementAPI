using NSubstitute;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Tests.Unit;

public class TeamServiceTests : ServiceTestBase
{
    private readonly TeamService _sut;
    private readonly Team _team = new() { Id = 10, Name = "Engineering", ManagerId = Manager.Id, Manager = Manager };

    public TeamServiceTests()
    {
        _sut = new TeamService(Uow, Current, Notifier);
        Teams.GetWithMembersAsync(_team.Id, Arg.Any<CancellationToken>()).Returns(_team);
    }

    [Fact]
    public async Task AddMembers_ByTeamManager_AddsUserAndNotifies()
    {
        ActAs(Manager);
        Users.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { Member });

        await _sut.AddMembersAsync(_team.Id, new AddTeamMembersRequest { UserIds = [Member.Id] });

        await Teams.Received(1).AddMemberAsync(Arg.Is<TeamMember>(m => m.TeamId == _team.Id && m.UserId == Member.Id), Arg.Any<CancellationToken>());
        await Notifier.Received(1).NotifyAddedToTeamAsync(_team, Member, Manager, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddMembers_ByManagerOfAnotherTeam_IsForbidden()
    {
        ActAs(OtherManager);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.AddMembersAsync(_team.Id, new AddTeamMembersRequest { UserIds = [Member.Id] }));
    }

    [Fact]
    public async Task AddMembers_ManagerAddingAnotherManager_IsForbidden()
    {
        ActAs(Manager);
        Users.GetByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { OtherManager });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.AddMembersAsync(_team.Id, new AddTeamMembersRequest { UserIds = [OtherManager.Id] }));
    }

    [Fact]
    public async Task Create_WithDuplicateName_IsConflict()
    {
        ActAs(Admin);
        Teams.NameExistsAsync("Engineering", null, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CreateAsync(new CreateTeamRequest { Name = "Engineering" }));
    }

    [Fact]
    public async Task Create_WithNonManagerAsManager_IsBadRequest()
    {
        ActAs(Admin);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreateAsync(new CreateTeamRequest { Name = "Design", ManagerId = Member.Id }));
    }
}
