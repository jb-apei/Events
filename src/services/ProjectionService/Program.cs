using Microsoft.EntityFrameworkCore;
using ProjectionService.Data;
using ProjectionService.EventHandlers;
using ProjectionService.Services;
using Shared.Infrastructure.Telemetry;
using Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Telemetry (OpenTelemetry + Application Insights)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
builder.Services.AddTelemetry("ProjectionService", appInsightsConnectionString);

// Add configuration sources
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets<Program>(optional: true);

// Configure DbContext
var connectionString = builder.Configuration.GetConnectionString("ProjectionDatabase")
    ?? throw new InvalidOperationException("ProjectionDatabase connection string not found");

builder.Services.AddDbContext<ProjectionDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

// Register event handlers
builder.Services.AddScoped<ProspectEventHandler>();
builder.Services.AddScoped<StudentEventHandler>();
builder.Services.AddScoped<InstructorEventHandler>();
builder.Services.AddScoped<EventDispatcher>();

// Add controllers for webhook endpoint
builder.Services.AddControllers();

// Add CORS for Event Grid webhook validation
builder.Services.AddCors(options =>
{
    options.AddPolicy("EventGridPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add background services
builder.Services.AddHostedService<InboxCleanupService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProjectionDbContext>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Apply migrations on startup (in production, use separate migration job)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProjectionDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations");
        throw;
    }
}

// Configure middleware
// Add Correlation ID middleware
app.UseCorrelationId();
app.UseCors("EventGridPolicy");
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
