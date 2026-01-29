using Microsoft.EntityFrameworkCore;
using ProspectService.Handlers;
using ProspectService.Infrastructure;
using ProspectService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for development mode event pushing
builder.Services.AddHttpClient();

// Configure Database Context
var connectionString = builder.Configuration.GetConnectionString("ProspectDb");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<ProspectDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // Use in-memory database for development if connection string not configured
    builder.Services.AddDbContext<ProspectDbContext>(options =>
        options.UseInMemoryDatabase("ProspectDb"));

    Console.WriteLine("WARNING: Using in-memory database. Configure ConnectionStrings:ProspectDb for production.");
}

// Register command handlers
builder.Services.AddScoped<CreateProspectCommandHandler>();
builder.Services.AddScoped<UpdateProspectCommandHandler>();

// Register Service Bus consumer as background service only if connection string configured
var serviceBusConnectionString = builder.Configuration["ServiceBus:ConnectionString"];
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    builder.Services.AddHostedService<ServiceBusCommandConsumer>();
    Console.WriteLine("INFO: Service Bus command consumer enabled");
}
else
{
    Console.WriteLine("INFO: Service Bus command consumer disabled (development mode)");
}

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProspectDbContext>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<ProspectDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();
