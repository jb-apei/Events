using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OutboxController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OutboxController> _logger;

    public OutboxController(IConfiguration configuration, ILogger<OutboxController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get all outbox messages for debugging
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxMessages()
    {
        try
        {
            // Query ProspectService database for outbox messages
            var prospectDbConnectionString = _configuration.GetConnectionString("ProspectDb");
            if (string.IsNullOrEmpty(prospectDbConnectionString))
            {
                _logger.LogWarning("ProspectDb connection string not configured");
                return Ok(Array.Empty<object>());
            }

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(prospectDbConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT TOP 100 
                    Id, 
                    EventId,
                    EventType, 
                    Payload, 
                    CreatedAt, 
                    Published,
                    PublishedAt
                FROM Outbox
                ORDER BY CreatedAt DESC";

            var outboxMessages = new List<object>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                outboxMessages.Add(new
                {
                    id = reader.GetInt64(0),
                    eventId = reader.GetString(1),
                    eventType = reader.GetString(2),
                    payload = reader.GetString(3),
                    createdAt = reader.GetDateTime(4),
                    published = reader.GetBoolean(5),
                    publishedAt = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
                });
            }

            _logger.LogInformation("Retrieved {Count} outbox messages", outboxMessages.Count);
            return Ok(outboxMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query outbox from ProspectService database");
            return StatusCode(500, new { error = "Failed to retrieve outbox messages", message = ex.Message });
        }
    }
}
