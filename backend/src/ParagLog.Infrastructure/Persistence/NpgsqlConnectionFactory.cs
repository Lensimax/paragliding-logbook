using Npgsql;

namespace ParagLog.Infrastructure.Persistence;

public sealed class NpgsqlConnectionFactory(string connectionString)
{
    public async Task<NpgsqlConnection> CreateOpenAsync(CancellationToken ct)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
