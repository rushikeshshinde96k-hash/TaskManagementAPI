using System.Net;
using System.Net.Http.Json;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Integration;

public class AuthApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndRole()
    {
        var auth = await ApiFactory.LoginAsync(_factory.CreateClient(), "manager@test.com");

        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));
        Assert.True(auth.AccessTokenExpiresAt > DateTime.UtcNow);
        Assert.Equal(UserRole.Manager, auth.User.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = "manager@test.com", password = "nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400WithErrors()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "A", email = "not-an-email", password = "weak", confirmPassword = "other" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("errors", body);
    }

    [Fact]
    public async Task Register_CreatesUserRoleAccount()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New Person", email = "new.person@test.com", password = "Str0ng!Pass", confirmPassword = "Str0ng!Pass"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(ApiFactory.Json);
        Assert.Equal(UserRole.User, auth!.User.Role);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.e30.invalid");

        var response = await client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndOldTokenCannotBeReused()
    {
        var client = _factory.CreateClient();
        var login = await ApiFactory.LoginAsync(client, "member@test.com");

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var refreshed = await first.Content.ReadFromJsonAsync<AuthResponse>(ApiFactory.Json);
        Assert.NotEqual(login.RefreshToken, refreshed!.RefreshToken);

        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var client = _factory.CreateClient();
        var login = await ApiFactory.LoginAsync(client, "outsider@test.com");

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
