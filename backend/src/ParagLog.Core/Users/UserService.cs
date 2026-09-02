using System.Security.Cryptography;
using System.Text;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Common;
using ParagLog.Core.Common.Validation;

namespace ParagLog.Core.Users;

public sealed record AuthenticatedSession(User User, string Token, DateTimeOffset ExpiresAt);

public sealed class UserService(
    IUserRepository users,
    ISessionRepository sessions,
    IPasswordHasher passwordHasher,
    string publicIdServerSalt)
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(12);
    private static readonly TimeSpan PersistentLifetime = TimeSpan.FromDays(30);

    public async Task<Result<AuthenticatedSession>> RegisterAsync(
        string username, string email, string password, string passwordConfirmation, CancellationToken ct)
    {
        if (!UsernameRules.IsValid(username))
            return Result<AuthenticatedSession>.Failure(
                DomainError.Validation("Username must be 3-20 characters: letters, digits, underscore or hyphen.", "username"));

        if (!EmailRules.IsValid(email))
            return Result<AuthenticatedSession>.Failure(DomainError.Validation("Email is not valid.", "email"));

        if (password != passwordConfirmation)
            return Result<AuthenticatedSession>.Failure(
                DomainError.Validation("Passwords do not match.", "passwordConfirmation"));

        if (!PasswordRules.HasMinLength(password))
            return Result<AuthenticatedSession>.Failure(
                DomainError.Validation($"Password must be at least {PasswordRules.MinLength} characters.", "password"));

        if (PasswordRules.IsCommonPassword(password))
            return Result<AuthenticatedSession>.Failure(
                DomainError.Validation("Password is too common. Choose a different one.", "password"));

        if (await users.UsernameExistsAsync(username, ct))
            return Result<AuthenticatedSession>.Failure(DomainError.Conflict("Username is already taken.", "username"));

        if (await users.EmailExistsAsync(email, ct))
            return Result<AuthenticatedSession>.Failure(DomainError.Conflict("Email is already registered.", "email"));

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            PublicId = PublicIdGenerator.Generate(username, now, publicIdServerSalt),
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            CreatedAt = now,
        };

        var created = await users.CreateAsync(user, ct);
        var (token, expiresAt) = await IssueSessionAsync(created.Id, persistent: false, ct);

        return Result<AuthenticatedSession>.Success(new AuthenticatedSession(created, token, expiresAt));
    }

    public async Task<Result<AuthenticatedSession>> LoginAsync(
        string email, string password, bool stayConnected, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(email, ct);
        if (user is null || user.PasswordHash is null || !passwordHasher.Verify(password, user.PasswordHash))
            return Result<AuthenticatedSession>.Failure(DomainError.Unauthorized("Invalid email or password."));

        var (token, expiresAt) = await IssueSessionAsync(user.Id, stayConnected, ct);
        return Result<AuthenticatedSession>.Success(new AuthenticatedSession(user, token, expiresAt));
    }

    public Task LogoutAsync(Guid userId, Guid sessionId, CancellationToken ct) =>
        sessions.DeleteAsync(userId, sessionId, ct);

    public async Task<Result<(User User, Guid SessionId)>> ValidateSessionAsync(string rawToken, CancellationToken ct)
    {
        var session = await sessions.FindByTokenHashAsync(HashToken(rawToken), ct);
        if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
            return Result<(User, Guid)>.Failure(DomainError.Unauthorized("Session is invalid or expired."));

        var user = await users.FindByIdAsync(session.UserId, ct);
        if (user is null)
            return Result<(User, Guid)>.Failure(DomainError.Unauthorized("Session is invalid or expired."));

        if (session.Persistent)
            await sessions.UpdateExpiryAsync(user.Id, session.Id, DateTimeOffset.UtcNow.Add(PersistentLifetime), ct);

        return Result<(User, Guid)>.Success((user, session.Id));
    }

    private async Task<(string Token, DateTimeOffset ExpiresAt)> IssueSessionAsync(
        Guid userId, bool persistent, CancellationToken ct)
    {
        var token = GenerateOpaqueToken();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(persistent ? PersistentLifetime : DefaultLifetime);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(token),
            CreatedAt = now,
            ExpiresAt = expiresAt,
            Persistent = persistent,
        };

        await sessions.CreateAsync(session, ct);
        return (token, expiresAt);
    }

    private static string GenerateOpaqueToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
