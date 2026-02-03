using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Shared.Infrastructure.Telemetry;

/// <summary>
/// Configuration extensions for OpenTelemetry and observability.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry with Application Insights integration.
    /// </summary>
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        string serviceName,
        string? applicationInsightsConnectionString = null)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("deployment.environment",
                        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request_id", request.HttpContext.TraceIdentifier);

                        // Add correlation ID if present
                        if (request.HttpContext.Items.TryGetValue("CorrelationId", out var correlationId))
                        {
                            activity.SetTag("correlation_id", correlationId?.ToString());
                        }
                    };
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                })
                .AddSource(serviceName)
                .AddSource("Azure.Messaging.ServiceBus")
                .AddSource("Azure.Messaging.EventGrid"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(serviceName));

        // Add Application Insights if connection string is provided
        if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
        {
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = applicationInsightsConnectionString;
            });
        }

        return services;
    }

    /// <summary>
    /// Adds standard health checks for Azure services.
    /// </summary>
    public static IHealthChecksBuilder AddAzureHealthChecks(
        this IHealthChecksBuilder builder,
        string? serviceBusConnectionString = null)
    {
        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            builder.AddCheck("service_bus",
                new HealthChecks.ServiceBusHealthCheck(serviceBusConnectionString),
                tags: new[] { "ready" });
        }

        return builder;
    }
}
