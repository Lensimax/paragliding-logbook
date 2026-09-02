using ParagLog.Core.Users;

namespace ParagLog.Core.Abstractions;

public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session, CancellationToken ct);
    Task<Session?> FindByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task DeleteAsync(Guid userId, Guid sessionId, CancellationToken ct);
    Task UpdateExpiryAsync(Guid userId, Guid sessionId, DateTimeOffset expiresAt, CancellationToken ct);
}
