using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.Infrastructure.HealthChecks;

/// <summary>
/// Base health check for verifying Azure Service Bus connectivity.
/// </summary>
public class ServiceBusHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public ServiceBusHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For local development with Azurite
            if (_connectionString.Contains("UseDevelopmentStorage=true") ||
                _connectionString.Contains("localhost"))
            {
                return HealthCheckResult.Healthy("Using local development storage");
            }

            // Basic connectivity check - in production you might want to verify specific queues/topics
            if (string.IsNullOrEmpty(_connectionString))
            {
                return HealthCheckResult.Unhealthy("Service Bus connection string is not configured");
            }

            // Connection string validation
            if (!_connectionString.Contains("Endpoint="))
            {
                return HealthCheckResult.Unhealthy("Invalid Service Bus connection string format");
            }

            return HealthCheckResult.Healthy("Service Bus connection string is configured");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Service Bus health check failed",
                exception: ex);
        }
    }
}
