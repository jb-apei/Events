using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using System.Diagnostics;

namespace Shared.Infrastructure.Middleware;

/// <summary>
/// Middleware to capture and propagate correlation IDs across service boundaries.
/// Ensures all logs and telemetry share the same correlation ID for distributed tracing.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if correlation ID is already present in request headers
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        // If not present, generate a new one
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        // Add to response headers for downstream services
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add to current activity for OpenTelemetry
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag("correlation_id", correlationId);
        }

        // Store in HttpContext for easy access
        context.Items["CorrelationId"] = correlationId;

        await _next(context);
    }
}

/// <summary>
/// Extension method to register the CorrelationIdMiddleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
