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

        await _commandPublisher.PublishCommandAsync(command, correlationId);

        _logger.LogInformation("CreateProspect command published with CorrelationId {CorrelationId}",
            correlationId);

        return AcceptedAtAction(
            nameof(GetProspect),
            new { id = correlationId },
            new { correlationId, message = "Prospect creation initiated" });
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
    public async Task<IActionResult> GetProspects([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // In development mode, proxy to ProspectService
            var prospectServiceUrl = _configuration["ProspectService:Url"] ?? "http://localhost:5110";
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync($"{prospectServiceUrl}/api/prospects");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            _logger.LogWarning("ProspectService returned {StatusCode}", response.StatusCode);
            return Ok(Array.Empty<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query ProspectService");
            return Ok(Array.Empty<object>());
        }
    }
}
