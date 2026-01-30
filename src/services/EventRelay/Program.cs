using EventRelay.Infrastructure;
using EventRelay.OutboxRelay;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Telemetry;

var builder = Host.CreateApplicationBuilder(args);

// Add Telemetry (OpenTelemetry + Application Insights)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
builder.Services.AddTelemetry("EventRelay", appInsightsConnectionString);

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddAzureHealthChecks(builder.Configuration["ServiceBus:ConnectionString"]);

// Database context for Outbox table
builder.Services.AddDbContext<OutboxDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ProspectDb");
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
