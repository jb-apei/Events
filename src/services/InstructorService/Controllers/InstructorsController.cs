using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InstructorService.Commands;
using InstructorService.Handlers;
using InstructorService.Infrastructure;
using InstructorService.Models;

namespace InstructorService.Controllers;

/// <summary>
/// API endpoints for Instructor operations (for testing and direct API access).
/// In production, commands should primarily come through Service Bus.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InstructorsController : ControllerBase
{
    private readonly CreateInstructorCommandHandler _createHandler;
    private readonly UpdateInstructorCommandHandler _updateHandler;
    private readonly InstructorDbContext _dbContext;
    private readonly ILogger<InstructorsController> _logger;

    public InstructorsController(
        CreateInstructorCommandHandler createHandler,
        UpdateInstructorCommandHandler updateHandler,
        InstructorDbContext dbContext,
        ILogger<InstructorsController> logger)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new instructor.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateInstructor([FromBody] CreateInstructorCommand command)
    {
        // Set correlation ID from header or generate new one
        if (string.IsNullOrEmpty(command.CorrelationId))
        {
            command.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();
        }

        _logger.LogInformation("Received CreateInstructor request: CorrelationId={CorrelationId}",
            command.CorrelationId);

        var result = await _createHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(
            nameof(GetInstructor),
            new { id = result.Value },
            new { instructorId = result.Value, correlationId = command.CorrelationId });
    }

    /// <summary>
    /// Update an existing instructor.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInstructor(int id, [FromBody] UpdateInstructorCommand command)
    {
        // Ensure ID in URL matches command
        command.InstructorId = id;

        // Set correlation ID from header or generate new one
        if (string.IsNullOrEmpty(command.CorrelationId))
        {
            command.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();
        }

        _logger.LogInformation("Received UpdateInstructor request: CorrelationId={CorrelationId}, InstructorId={InstructorId}",
            command.CorrelationId, id);

        var result = await _updateHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { instructorId = result.Value, correlationId = command.CorrelationId });
    }

    /// <summary>
    /// Get all instructors (for testing - reads from write model database).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllInstructors()
    {
        var instructors = await _dbContext.Instructors
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        _logger.LogInformation("GetAllInstructors returned {Count} instructors", instructors.Count);

        // Convert to DTOs with consistent property naming
        var instructorDtos = instructors.ToDtoList();

        return Ok(instructorDtos);
    }

    /// <summary>
    /// Get an instructor by ID (for testing - reads from write model database).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetInstructor(int id)
    {
        var instructor = await _dbContext.Instructors.FindAsync(id);

        if (instructor == null)
        {
            return NotFound(new { message = "Instructor not found", instructorId = id });
        }

        _logger.LogInformation("GetInstructor returned InstructorId={InstructorId}", id);

        // Convert to DTO with consistent property naming
        var instructorDto = instructor.ToDto();

        return Ok(instructorDto);
    }
}
