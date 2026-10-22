using System.Text.Json.Serialization;
using TaskManagement.Application;
using TaskManagement.Application.Interfaces.Common;
using TaskManagement.Infrastructure;
using TaskManagementAPI.Extensions;
using TaskManagementAPI.Middleware;
using TaskManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    });

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddFrontendCors(builder.Configuration);
builder.Services.AddApiDocumentation();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// API docs are exposed in every environment so reviewers can try the API: /swagger
app.MapOpenApi();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "Task Management API v1");
    o.RoutePrefix = "swagger";
    o.DocumentTitle = "Task Management API";
});

if (!app.Environment.IsDevelopment() && app.Configuration.GetValue("UseHttpsRedirection", true))
    app.UseHttpsRedirection();

app.UseCors(ServiceCollectionExtensions.CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", time = DateTime.UtcNow })).AllowAnonymous();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
