using Dapper;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Users;

namespace ParagLog.Infrastructure.Persistence;

public sealed class SessionRepository(NpgsqlConnectionFactory connectionFactory) : ISessionRepository
{
    private static readonly string InsertSql = SqlLoader.Load("Sessions.Insert");
    private static readonly string FindByTokenHashSql = SqlLoader.Load("Sessions.FindByTokenHash");
    private static readonly string DeleteSql = SqlLoader.Load("Sessions.Delete");
    private static readonly string UpdateExpirySql = SqlLoader.Load("Sessions.UpdateExpiry");

    public async Task<Session> CreateAsync(Session session, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(
            InsertSql,
            new
            {
                session.Id,
                session.UserId,
                session.TokenHash,
                session.CreatedAt,
                session.ExpiresAt,
                session.Persistent,
                session.UserAgent,
                session.IpHash,
            },
            cancellationToken: ct);
        await connection.ExecuteAsync(command);
        return session;
    }

    public async Task<Session?> FindByTokenHashAsync(string tokenHash, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(FindByTokenHashSql, new { TokenHash = tokenHash }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<Session>(command);
    }

    public async Task DeleteAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(DeleteSql, new { UserId = userId, SessionId = sessionId }, cancellationToken: ct);
        await connection.ExecuteAsync(command);
    }

    public async Task UpdateExpiryAsync(Guid userId, Guid sessionId, DateTimeOffset expiresAt, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(
            UpdateExpirySql,
            new { UserId = userId, SessionId = sessionId, ExpiresAt = expiresAt },
            cancellationToken: ct);
        await connection.ExecuteAsync(command);
    }
}
