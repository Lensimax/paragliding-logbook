using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Users;
using ParagLog.Infrastructure.Persistence;
using ParagLog.Infrastructure.Security;

namespace ParagLog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default configuration.");

        var publicIdServerSalt = configuration["Accounts:PublicIdServerSalt"]
            ?? throw new InvalidOperationException("Missing Accounts:PublicIdServerSalt configuration.");

        services.AddSingleton(new NpgsqlConnectionFactory(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

        services.AddScoped(sp => new UserService(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<ISessionRepository>(),
            sp.GetRequiredService<IPasswordHasher>(),
            publicIdServerSalt));

        return services;
    }
}
