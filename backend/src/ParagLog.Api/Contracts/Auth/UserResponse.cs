using ParagLog.Core.Users;

namespace ParagLog.Api.Contracts.Auth;

public sealed record UserResponse(Guid Id, string PublicId, string Username, string Email)
{
    public static UserResponse From(User user) => new(user.Id, user.PublicId, user.Username, user.Email);
}
