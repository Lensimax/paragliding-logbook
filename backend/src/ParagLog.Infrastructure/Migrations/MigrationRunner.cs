using DbUp;
using DbUp.Engine;

namespace ParagLog.Infrastructure.Migrations;

public static class MigrationRunner
{
    public static DatabaseUpgradeResult Run(string connectionString)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(MigrationRunner).Assembly,
                name => name.Contains(".Migrations.", StringComparison.Ordinal))
            .LogToConsole()
            .Build();

        return upgrader.PerformUpgrade();
    }
}
