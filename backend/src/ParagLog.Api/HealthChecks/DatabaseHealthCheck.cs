using Microsoft.Extensions.Diagnostics.HealthChecks;
using ParagLog.Infrastructure.Persistence;

namespace ParagLog.Api.HealthChecks;

public sealed class DatabaseHealthCheck(NpgsqlConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await connectionFactory.CreateOpenAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.", ex);
        }
    }
}
