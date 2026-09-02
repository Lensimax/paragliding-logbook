using ParagLog.Infrastructure.Migrations;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:Default configuration.");

var migrationResult = MigrationRunner.Run(connectionString);
if (!migrationResult.Successful)
    throw new InvalidOperationException("Database migration failed.", migrationResult.Error);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
