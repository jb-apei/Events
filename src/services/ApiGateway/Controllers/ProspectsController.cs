using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProspectsController : ControllerBase
{
    private readonly CommandPublisher _commandPublisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProspectsController> _logger;

    public ProspectsController(
        CommandPublisher commandPublisher,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ProspectsController> logger)
    {
        _commandPublisher = commandPublisher;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Create a new prospect
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProspect([FromBody] CreateProspectRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        var command = new CommandMessage<CreateProspectRequest>
        {
            CommandType = "CreateProspect",
            CorrelationId = correlationId,
            UserId = userId,
            Payload = request
        };

        try
        {
            await _commandPublisher.PublishCommandAsync(command, correlationId);

            _logger.LogInformation("CreateProspect command published with CorrelationId {CorrelationId}",
                correlationId);

            return AcceptedAtAction(
                nameof(GetProspect),
                new { id = correlationId },
                new { correlationId, message = "Prospect creation initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish CreateProspect command: {Message}", ex.Message);
            return StatusCode(500, new { error = "Failed to initiate prospect creation", details = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing prospect
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProspect(int id, [FromBody] UpdateProspectRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        var payload = new
        {
            ProspectId = id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.Notes
        };

        var command = new CommandMessage<object>
        {
            CommandType = "UpdateProspect",
            CorrelationId = correlationId,
            UserId = userId,
            Payload = payload
        };

        await _commandPublisher.PublishCommandAsync(command, correlationId);

        _logger.LogInformation("UpdateProspect command published for ProspectId {ProspectId} with CorrelationId {CorrelationId}",
            id, correlationId);

        return AcceptedAtAction(
            nameof(GetProspect),
            new { id },
            new { correlationId, message = "Prospect update initiated" });
    }

    /// <summary>
    /// Get prospect by ID (placeholder - would query read model)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProspect(int id)
    {
        // TODO: Query read model database
        // This is a placeholder for MVP - would connect to ProjectionService read model
        await Task.CompletedTask;

        _logger.LogInformation("GetProspect called for ProspectId {ProspectId}", id);

        return Ok(new
        {
            prospectId = id,
            message = "Read model query not implemented in MVP - use event subscriptions for real-time updates"
        });
    }

    /// <summary>
    /// Get all prospects (queries ProspectService for development)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProspects([FromQuery] int page = 1, [FromQuery] int pageSize = 1000)
    {
        try
        {
            // Query the read model database directly
            var readModelConnectionString = _configuration.GetConnectionString("ReadModelDb");
            if (string.IsNullOrEmpty(readModelConnectionString))
            {
                _logger.LogWarning("ReadModelDb connection string not configured");
                return Ok(Array.Empty<object>());
            }

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(readModelConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ProspectId, FirstName, LastName, Email, Phone, Status, Notes, CreatedAt, UpdatedAt
                FROM ProspectSummary
                ORDER BY CreatedAt DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            var prospects = new List<object>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                prospects.Add(new
                {
                    prospectId = reader.GetInt32(0),
                    firstName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    lastName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    email = reader.IsDBNull(3) ? null : reader.GetString(3),
                    phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                    status = reader.IsDBNull(5) ? null : reader.GetString(5),
                    notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                    createdAt = reader.GetDateTime(7),
                    updatedAt = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8)
                });
            }

            _logger.LogInformation("Retrieved {Count} prospects from read model", prospects.Count);
            return Ok(prospects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query read model database");
            return StatusCode(500, new { error = "Failed to retrieve prospects", details = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}
