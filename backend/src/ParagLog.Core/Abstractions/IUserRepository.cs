using ParagLog.Core.Users;

namespace ParagLog.Core.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid userId, CancellationToken ct);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<User> CreateAsync(User user, CancellationToken ct);
}
