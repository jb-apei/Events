using ApiGateway.EventHandlers;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Shared.Infrastructure.Telemetry;
using Shared.Infrastructure.Middleware;
using WsManager = ApiGateway.WebSockets.WebSocketManager;
using WsHandler = ApiGateway.WebSockets.WebSocketHandler;

var builder = WebApplication.CreateBuilder(args);

// Add Telemetry (OpenTelemetry + Application Insights)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
builder.Services.AddTelemetry("ApiGateway", appInsightsConnectionString);

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddAzureHealthChecks(builder.Configuration["ServiceBus:ConnectionString"]);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrEmpty(jwtSecretKey))
{
    // For development only - generate a temporary key
    jwtSecretKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + Guid.NewGuid().ToString()));
    builder.Configuration["Jwt:SecretKey"] = jwtSecretKey;
    Console.WriteLine("WARNING: Using temporary JWT secret key. Configure Jwt:SecretKey in production.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EventsApiGateway",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EventsClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Allow WebSocket authentication via query parameter
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Register HTTP client for development mode
builder.Services.AddHttpClient();

// Register custom services
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<CommandPublisher>();
builder.Services.AddSingleton<WsManager>();
builder.Services.AddSingleton<WsHandler>();
builder.Services.AddSingleton<EventGridWebhookHandler>();

// Configure CORS (adjust for production)
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
// Add Correlation ID middleware (must be early in pipeline)
app.UseCorrelationId();

// Enable Swagger for all environments (Development, Staging, Production)
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); // Disabled for Container Apps

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Configure WebSocket support
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("WebSockets:KeepAliveInterval", 120))
};
app.UseWebSockets(webSocketOptions);

// WebSocket endpoint
app.Map("/ws/events", async context =>
{
    var isDevelopment = app.Environment.IsDevelopment();

    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        // Try to authenticate via query parameter token
        var token = context.Request.Query["token"].ToString();
        if (!string.IsNullOrEmpty(token))
        {
            var jwtService = context.RequestServices.GetRequiredService<JwtService>();
            var principal = jwtService.ValidateToken(token);
            if (principal != null)
            {
                context.User = principal;
            }
        }
    }

    // In development, allow unauthenticated connections for easier testing
    if (!isDevelopment && (!context.User.Identity?.IsAuthenticated ?? true))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
    var handler = context.RequestServices.GetRequiredService<WsHandler>();
    await handler.HandleWebSocketAsync(context, userId);
});

// Health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

