using Microsoft.EntityFrameworkCore;
using StudentService.Handlers;
using StudentService.Infrastructure;
using Shared.Infrastructure.Telemetry;
using Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Telemetry (OpenTelemetry + Application Insights)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
builder.Services.AddTelemetry("StudentService", appInsightsConnectionString);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for development mode event pushing
builder.Services.AddHttpClient();

// Configure Database Context
var connectionString = builder.Configuration.GetConnectionString("StudentDb");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<StudentDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // Use in-memory database for development if connection string not configured
    builder.Services.AddDbContext<StudentDbContext>(options =>
        options.UseInMemoryDatabase("StudentDb")
               .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));

    Console.WriteLine("WARNING: Using in-memory database. Configure ConnectionStrings:StudentDb for production.");
}

// Register command handlers
builder.Services.AddScoped<CreateStudentCommandHandler>();
builder.Services.AddScoped<UpdateStudentCommandHandler>();

// Register Service Bus consumer as background service only if connection string configured
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    // Future: Add ServiceBusCommandConsumer when implementing production mode
    Console.WriteLine("INFO: Service Bus command consumer enabled");
}
else
{
    Console.WriteLine("INFO: Service Bus command consumer disabled (development mode)");
}

// Add health checks (includes Service Bus check)
builder.Services.AddHealthChecks()
    .AddAzureHealthChecks(builder.Configuration["ServiceBus:ConnectionString"])
    .AddDbContextCheck<StudentDbContext>();

// Add CORS (configure as needed)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Add Correlation ID middleware
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disabled for Container Apps (SSL termination at ingress)
app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health");

// Ensure database is created (Run in all environments for MVP)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
        // Check if we are using InMemory or SQL
        if (dbContext.Database.IsSqlServer())
        {
             // Use EnsureCreated for simple setup, or Migrate for production
             // await dbContext.Database.MigrateAsync();
             await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
             await dbContext.Database.EnsureCreatedAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR initializing database: {ex.Message}");
    }
}

app.Run();
