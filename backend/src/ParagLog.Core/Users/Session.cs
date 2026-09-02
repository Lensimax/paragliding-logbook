namespace ParagLog.Core.Users;

public sealed class Session
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }

    /// <summary>SHA-256 of the opaque cookie token. The raw token is never stored.</summary>
    public required string TokenHash { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>True when "Stay connected" was checked, giving the session a 30-day sliding expiration.</summary>
    public required bool Persistent { get; init; }

    public string? UserAgent { get; init; }
    public string? IpHash { get; init; }
}
