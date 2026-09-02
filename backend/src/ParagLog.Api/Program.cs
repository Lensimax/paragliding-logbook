using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ParagLog.Api.Auth;
using ParagLog.Api.Endpoints;
using ParagLog.Api.HealthChecks;
using ParagLog.Api.Middleware;
using ParagLog.Infrastructure;
using ParagLog.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

// JSON logs so a Docker log-collector (or Caddy/journald in front of it) can index fields
// instead of regex-parsing formatted text.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser>(sp => CurrentUser.FromHttpContext(sp.GetRequiredService<IHttpContextAccessor>()));

builder.Services
    .AddAuthentication(SessionCookieDefaults.Scheme)
    .AddScheme<SessionCookieAuthOptions, SessionCookieAuthHandler>(SessionCookieDefaults.Scheme, _ => { });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// Guards register/login against brute-force and credential-stuffing attempts. Partitioned per
// client IP so one attacker can't exhaust the quota for every other user.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many requests. Try again shortly." }, cancellationToken: ct);
    };
});

builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Default configuration.");

var migrationResult = MigrationRunner.Run(connectionString);
if (!migrationResult.Successful)
    throw new InvalidOperationException("Database migration failed.", migrationResult.Error);

app.UseExceptionHandler();
app.UseRequestLogging();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapActivityEndpoints();
app.MapEquipmentEndpoints();
app.MapTrackEndpoints();
app.MapExportEndpoints();

app.Run();

public partial class Program;
