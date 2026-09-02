using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParagLog.Core.Abstractions;
using ParagLog.Core.Activities;
using ParagLog.Core.Equipment;
using ParagLog.Core.Users;
using ParagLog.Infrastructure.Elevation;
using ParagLog.Infrastructure.Persistence;
using ParagLog.Infrastructure.Persistence.TypeHandlers;
using ParagLog.Infrastructure.Security;
using ParagLog.Infrastructure.Storage;

namespace ParagLog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:Default configuration.");

        var publicIdServerSalt = configuration["Accounts:PublicIdServerSalt"]
            ?? throw new InvalidOperationException("Missing Accounts:PublicIdServerSalt configuration.");

        var blobsRoot = configuration["Storage:BlobsRoot"]
            ?? throw new InvalidOperationException("Missing Storage:BlobsRoot configuration.");

        var elevationBaseUrl = configuration["Elevation:BaseUrl"]
            ?? throw new InvalidOperationException("Missing Elevation:BaseUrl configuration.");

        SqlMapper.AddTypeHandler(new PgEnumTypeHandler<ActivityType>());
        SqlMapper.AddTypeHandler(new PgEnumTypeHandler<TrackFormat>());
        SqlMapper.AddTypeHandler(new PgEnumTypeHandler<EquipmentType>());
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        services.AddSingleton(new NpgsqlConnectionFactory(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IBlobStore>(new FileSystemBlobStore(blobsRoot));

        services.AddHttpClient<IElevationService, OpenTopoDataClient>(client =>
        {
            client.BaseAddress = new Uri(elevationBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<ElevationResolver>();

        services.AddScoped(sp => new UserService(
            sp.GetRequiredService<IUserRepository>(),
            sp.GetRequiredService<ISessionRepository>(),
            sp.GetRequiredService<IPasswordHasher>(),
            publicIdServerSalt));

        services.AddScoped<ActivityService>();
        services.AddScoped<EquipmentService>();

        return services;
    }
}
