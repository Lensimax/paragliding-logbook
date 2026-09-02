using Dapper;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Users;

namespace ParagLog.Infrastructure.Persistence;

public sealed class UserRepository(NpgsqlConnectionFactory connectionFactory) : IUserRepository
{
    private static readonly string FindByIdSql = SqlLoader.Load("Users.FindById");
    private static readonly string FindByEmailSql = SqlLoader.Load("Users.FindByEmail");
    private static readonly string UsernameExistsSql = SqlLoader.Load("Users.UsernameExists");
    private static readonly string EmailExistsSql = SqlLoader.Load("Users.EmailExists");
    private static readonly string InsertSql = SqlLoader.Load("Users.Insert");

    public async Task<User?> FindByIdAsync(Guid userId, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(FindByIdSql, new { UserId = userId }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<User>(command);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(FindByEmailSql, new { Email = email }, cancellationToken: ct);
        return await connection.QuerySingleOrDefaultAsync<User>(command);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(UsernameExistsSql, new { Username = username }, cancellationToken: ct);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(EmailExistsSql, new { Email = email }, cancellationToken: ct);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<User> CreateAsync(User user, CancellationToken ct)
    {
        await using var connection = await connectionFactory.CreateOpenAsync(ct);
        var command = new CommandDefinition(
            InsertSql,
            new
            {
                user.Id,
                user.PublicId,
                user.Username,
                user.Email,
                user.PasswordHash,
                user.GoogleSub,
                user.CreatedAt,
            },
            cancellationToken: ct);
        await connection.ExecuteAsync(command);
        return user;
    }
}
