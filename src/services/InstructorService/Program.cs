using Microsoft.EntityFrameworkCore;
using InstructorService.Handlers;
using InstructorService.Infrastructure;
using Shared.Infrastructure.Telemetry;
using Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Telemetry (OpenTelemetry + Application Insights)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
builder.Services.AddTelemetry("InstructorService", appInsightsConnectionString);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for development mode event pushing
builder.Services.AddHttpClient();

// Configure Database Context
var connectionString = builder.Configuration.GetConnectionString("InstructorDb");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<InstructorDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // Use in-memory database for development if connection string not configured
    builder.Services.AddDbContext<InstructorDbContext>(options =>
        options.UseInMemoryDatabase("InstructorDb")
               .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));

    Console.WriteLine("WARNING: Using in-memory database. Configure ConnectionStrings:InstructorDb for production.");
}

// Register command handlers
builder.Services.AddScoped<CreateInstructorCommandHandler>();
builder.Services.AddScoped<UpdateInstructorCommandHandler>();

// Register Service Bus consumer as background service only if connection string configured
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    // TODO: Add ServiceBusCommandConsumer when implementing Service Bus integration
    Console.WriteLine("INFO: Service Bus command consumer enabled");
}
else
{
    Console.WriteLine("INFO: Service Bus command consumer disabled (development mode)");
}

// Add health checks (includes Service Bus check)
builder.Services.AddHealthChecks()
    .AddAzureHealthChecks(builder.Configuration["ServiceBus:ConnectionString"])
    .AddDbContextCheck<InstructorDbContext>();

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

app.UseHttpsRedirection();
app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health");

// Ensure database is created (for development only - use migrations in production)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InstructorDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();
