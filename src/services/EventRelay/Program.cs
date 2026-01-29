using EventRelay.Infrastructure;
using EventRelay.OutboxRelay;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

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
