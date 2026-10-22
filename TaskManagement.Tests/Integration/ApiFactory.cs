using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaskManagement.Application.DTOs.Auth;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Security;

namespace TaskManagement.Tests.Integration;

/// <summary>Runs the real API pipeline against an isolated in-memory database with known seed data.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Password = "Passw0rd!";
    public const int AdminId = 1, ManagerId = 2, OtherManagerId = 3, MemberId = 4, OutsiderId = 5, TeamId = 1;

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _dbName = "tm-tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        Seed(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        return host;
    }

    private static void Seed(AppDbContext db)
    {
        var hash = new BCryptPasswordHasher().Hash(Password);
        db.Users.AddRange(
            new User { Id = AdminId, FullName = "Admin", Email = "admin@test.com", PasswordHash = hash, Role = UserRole.Admin },
            new User { Id = ManagerId, FullName = "Manager", Email = "manager@test.com", PasswordHash = hash, Role = UserRole.Manager },
            new User { Id = OtherManagerId, FullName = "Other Manager", Email = "other@test.com", PasswordHash = hash, Role = UserRole.Manager },
            new User { Id = MemberId, FullName = "Member", Email = "member@test.com", PasswordHash = hash, Role = UserRole.User },
            new User { Id = OutsiderId, FullName = "Outsider", Email = "outsider@test.com", PasswordHash = hash, Role = UserRole.User });
        db.Teams.Add(new Team { Id = TeamId, Name = "Engineering", ManagerId = ManagerId });
        db.TeamMembers.Add(new TeamMember { TeamId = TeamId, UserId = MemberId });
        db.SaveChanges();
    }

    public async Task<HttpClient> CreateClientAsAsync(string email)
    {
        var client = CreateClient();
        var auth = await LoginAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public static async Task<AuthResponse> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }
}
