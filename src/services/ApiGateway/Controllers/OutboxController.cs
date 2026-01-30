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
                    EventType, 
                    AggregateId, 
                    Payload, 
                    CreatedAt, 
                    PublishedAt
                FROM Outbox
                ORDER BY CreatedAt DESC";

            var outboxMessages = new List<object>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                outboxMessages.Add(new
                {
                    id = reader.GetGuid(0),
                    eventType = reader.GetString(1),
                    aggregateId = reader.GetInt32(2),
                    payload = reader.GetString(3),
                    createdAt = reader.GetDateTime(4),
                    publishedAt = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                    published = !reader.IsDBNull(5)
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
