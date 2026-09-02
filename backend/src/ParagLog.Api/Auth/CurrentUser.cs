using System.Security.Claims;

namespace ParagLog.Api.Auth;

public interface ICurrentUser
{
    Guid Id { get; }
    Guid SessionId { get; }
    string Username { get; }
    string PublicId { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    public required Guid Id { get; init; }
    public required Guid SessionId { get; init; }
    public required string Username { get; init; }
    public required string PublicId { get; init; }

    public static CurrentUser FromHttpContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User
            ?? throw new InvalidOperationException("No active HTTP context.");

        return new CurrentUser
        {
            Id = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing user id claim.")),
            SessionId = Guid.Parse(user.FindFirstValue("sid")
                ?? throw new InvalidOperationException("Missing session id claim.")),
            Username = user.FindFirstValue(ClaimTypes.Name)
                ?? throw new InvalidOperationException("Missing username claim."),
            PublicId = user.FindFirstValue("pid")
                ?? throw new InvalidOperationException("Missing public id claim."),
        };
    }
}
