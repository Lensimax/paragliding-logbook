using System.Text.Json;
using System.Text.Json.Serialization;
using ParagLog.Api.Auth;
using ParagLog.Api.Endpoints;
using ParagLog.Infrastructure;
using ParagLog.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser>(sp => CurrentUser.FromHttpContext(sp.GetRequiredService<IHttpContextAccessor>()));

builder.Services
    .AddAuthentication(SessionCookieDefaults.Scheme)
    .AddScheme<SessionCookieAuthOptions, SessionCookieAuthHandler>(SessionCookieDefaults.Scheme, _ => { });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Default configuration.");

var migrationResult = MigrationRunner.Run(connectionString);
if (!migrationResult.Successful)
    throw new InvalidOperationException("Database migration failed.", migrationResult.Error);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapActivityEndpoints();
app.MapEquipmentEndpoints();
app.MapTrackEndpoints();

app.Run();

public partial class Program;
