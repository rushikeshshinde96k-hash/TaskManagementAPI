using NSubstitute;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Security;

namespace TaskManagement.Tests.Unit;

public class AuthServiceTests : ServiceTestBase
{
    private readonly IPasswordHasher _hasher = new BCryptPasswordHasher();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokens.GenerateAccessToken(Arg.Any<User>()).Returns(new AccessTokenResult("access", DateTime.UtcNow.AddMinutes(30)));
        _tokens.GenerateRefreshToken().Returns("refresh");
        _tokens.HashToken(Arg.Any<string>()).Returns(ci => "hash:" + ci.Arg<string>());
        _tokens.RefreshTokenLifetimeDays.Returns(7);
        _sut = new AuthService(Uow, _hasher, _tokens);
    }

    [Fact]
    public async Task Register_AlwaysCreatesUserRole_AndHashesPassword()
    {
        User? saved = null;
        await Users.AddAsync(Arg.Do<User>(u => saved = u), Arg.Any<CancellationToken>());

        var result = await _sut.RegisterAsync(new RegisterRequest
        {
            FullName = "New Person", Email = "  New@Example.com ", Password = "Secret@123", ConfirmPassword = "Secret@123"
        });

        Assert.NotNull(saved);
        Assert.Equal(UserRole.User, saved!.Role);
        Assert.Equal("new@example.com", saved.Email);
        Assert.NotEqual("Secret@123", saved.PasswordHash);
        Assert.True(_hasher.Verify("Secret@123", saved.PasswordHash));
        Assert.Equal("access", result.AccessToken);
    }

    [Fact]
    public async Task Register_DuplicateEmail_IsConflict()
    {
        Users.EmailExistsAsync("taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.RegisterAsync(new RegisterRequest
        {
            FullName = "X Y", Email = "taken@example.com", Password = "Secret@123", ConfirmPassword = "Secret@123"
        }));
    }

    [Fact]
    public async Task Login_WrongPassword_IsUnauthorized()
    {
        Users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 7, Email = "a@b.com", PasswordHash = _hasher.Hash("Right@123"), IsActive = true });

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _sut.LoginAsync(new LoginRequest { Email = "a@b.com", Password = "Wrong@123" }));
    }

    [Fact]
    public async Task Login_DeactivatedAccount_IsForbidden()
    {
        Users.GetByEmailAsync("a@b.com", Arg.Any<CancellationToken>())
            .Returns(new User { Id = 7, Email = "a@b.com", PasswordHash = _hasher.Hash("Right@123"), IsActive = false });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.LoginAsync(new LoginRequest { Email = "a@b.com", Password = "Right@123" }));
    }

    [Fact]
    public async Task Refresh_RevokedToken_IsUnauthorized()
    {
        RefreshTokens.GetByTokenHashAsync("hash:old", Arg.Any<CancellationToken>())
            .Returns(new RefreshToken { Token = "hash:old", ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow, User = Member });

        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = "old" }));
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesToken()
    {
        var stored = new RefreshToken { Token = "hash:old", ExpiresAt = DateTime.UtcNow.AddDays(1), UserId = Member.Id, User = Member };
        RefreshTokens.GetByTokenHashAsync("hash:old", Arg.Any<CancellationToken>()).Returns(stored);
        Member.IsActive = true;

        var result = await _sut.RefreshAsync(new RefreshTokenRequest { RefreshToken = "old" });

        Assert.NotNull(stored.RevokedAt);
        Assert.Equal("refresh", result.RefreshToken);
        await RefreshTokens.Received(1).AddAsync(Arg.Is<RefreshToken>(r => r.Token == "hash:refresh"), Arg.Any<CancellationToken>());
    }
}
