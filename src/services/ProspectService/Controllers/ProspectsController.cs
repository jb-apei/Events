using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProspectService.Commands;
using ProspectService.Handlers;
using ProspectService.Infrastructure;
using ProspectService.Models;

namespace ProspectService.Controllers;

/// <summary>
/// API endpoints for Prospect operations (for testing and direct API access).
/// In production, commands should primarily come through Service Bus.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProspectsController : ControllerBase
{
    private readonly CreateProspectCommandHandler _createHandler;
    private readonly UpdateProspectCommandHandler _updateHandler;
    private readonly ProspectDbContext _dbContext;
    private readonly ILogger<ProspectsController> _logger;

    public ProspectsController(
        CreateProspectCommandHandler createHandler,
        UpdateProspectCommandHandler updateHandler,
        ProspectDbContext dbContext,
        ILogger<ProspectsController> logger)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new prospect.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProspect([FromBody] CreateProspectCommand command)
    {
        // Set correlation ID from header or generate new one
        if (string.IsNullOrEmpty(command.CorrelationId))
        {
            command.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();
        }

        _logger.LogInformation("Received CreateProspect request: CorrelationId={CorrelationId}",
            command.CorrelationId);

        var result = await _createHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(
            nameof(GetProspect),
            new { id = result.Value },
            new { prospectId = result.Value, correlationId = command.CorrelationId });
    }

    /// <summary>
    /// Update an existing prospect.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProspect(int id, [FromBody] UpdateProspectCommand command)
    {
        // Ensure ID in URL matches command
        command.ProspectId = id;

        // Set correlation ID from header or generate new one
        if (string.IsNullOrEmpty(command.CorrelationId))
        {
            command.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();
        }

        _logger.LogInformation("Received UpdateProspect request: CorrelationId={CorrelationId}, ProspectId={ProspectId}",
            command.CorrelationId, id);

        var result = await _updateHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { prospectId = result.Value, correlationId = command.CorrelationId });
    }

    /// <summary>
    /// Get all prospects (for testing - reads from write model database).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllProspects()
    {
        var prospects = await _dbContext.Prospects
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        _logger.LogInformation("GetAllProspects returned {Count} prospects", prospects.Count);

        // Convert to DTOs with consistent property naming
        var prospectDtos = prospects.ToDtoList();

        return Ok(prospectDtos);
    }

    /// <summary>
    /// Get a prospect by ID (for testing - reads from write model database).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProspect(int id)
    {
        var prospect = await _dbContext.Prospects.FindAsync(id);

        if (prospect == null)
        {
            return NotFound(new { message = "Prospect not found", prospectId = id });
        }

        _logger.LogInformation("GetProspect returned ProspectId={ProspectId}", id);

        // Convert to DTO with consistent property naming
        var prospectDto = prospect.ToDto();

        return Ok(prospectDto);
    }
}
