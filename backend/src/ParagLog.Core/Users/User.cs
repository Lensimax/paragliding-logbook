namespace ParagLog.Core.Users;

public sealed class User
{
    public required Guid Id { get; init; }
    public required string PublicId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? PasswordHash { get; init; }
    public string? GoogleSub { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
