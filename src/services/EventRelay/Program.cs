using EventRelay.Infrastructure;
using EventRelay.OutboxRelay;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Telemetry;

var builder = Host.CreateApplicationBuilder(args);

// Add Telemetry (OpenTelemetry + Application Insights)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
builder.Services.AddTelemetry("EventRelay", appInsightsConnectionString);

// Add Health Checks
builder.Services.AddHealthChecks();

// Database context for Outbox table
builder.Services.AddDbContext<OutboxDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("OutboxDb")
                           ?? builder.Configuration.GetConnectionString("ProspectDb");

    if (string.IsNullOrEmpty(connectionString))
    {
         // Fallback for local development or if config is missing
         throw new InvalidOperationException("Connection string 'OutboxDb' or 'ProspectDb' not found in configuration.");
    }

    options.UseSqlServer(connectionString);
});

// Event Grid publisher
builder.Services.AddSingleton<IEventPublisher, EventGridPublisher>();

// Background service
builder.Services.AddHostedService<OutboxRelayService>();

// Logging
builder.Logging.AddConsole();

var host = builder.Build();
host.Run();
