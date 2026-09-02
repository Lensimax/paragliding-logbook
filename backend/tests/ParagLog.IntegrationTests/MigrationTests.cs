using ParagLog.Infrastructure.Migrations;
using Testcontainers.PostgreSql;

namespace ParagLog.IntegrationTests;

public class MigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public void Migrations_apply_cleanly_to_a_fresh_database()
    {
        var result = MigrationRunner.Run(_postgres.GetConnectionString());

        Assert.True(result.Successful, result.Error?.ToString());
    }

    [Fact]
    public void Migrations_are_idempotent()
    {
        var connectionString = _postgres.GetConnectionString();

        MigrationRunner.Run(connectionString);
        var second = MigrationRunner.Run(connectionString);

        Assert.True(second.Successful, second.Error?.ToString());
        Assert.Empty(second.Scripts);
    }
}
