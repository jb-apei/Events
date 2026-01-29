using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentService.Commands;
using StudentService.Handlers;
using StudentService.Infrastructure;
using StudentService.Models;

namespace StudentService.Controllers;

/// <summary>
/// API endpoints for Student operations (for testing and direct API access).
/// In production, commands should primarily come through Service Bus.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly CreateStudentCommandHandler _createHandler;
    private readonly UpdateStudentCommandHandler _updateHandler;
    private readonly StudentDbContext _dbContext;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        CreateStudentCommandHandler createHandler,
        UpdateStudentCommandHandler updateHandler,
        StudentDbContext dbContext,
        ILogger<StudentsController> logger)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new student.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentCommand command)
    {
        try
        {
            // Set correlation ID from header or generate new one
            if (string.IsNullOrEmpty(command.CorrelationId))
            {
                command.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString();
            }

            _logger.LogInformation("Received CreateStudent request: CorrelationId={CorrelationId}",
                command.CorrelationId);

            var result = await _createHandler.HandleAsync(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return CreatedAtAction(
                nameof(GetStudent),
                new { id = result.Value },
                new { studentId = result.Value, correlationId = command.CorrelationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            return StatusCode(500, new { message = "Error creating student", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Update an existing student.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentCommand command)
    {
        // Ensure ID in URL matches command
        command.StudentId = id;

        // Set correlation ID from header or generate new one
        if (string.IsNullOrEmpty(command.CorrelationId))
        {
            command.CorrelationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();
        }

        _logger.LogInformation("Received UpdateStudent request: CorrelationId={CorrelationId}, StudentId={StudentId}",
            command.CorrelationId, id);

        var result = await _updateHandler.HandleAsync(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { studentId = result.Value, correlationId = command.CorrelationId });
    }

    /// <summary>
    /// Get all students (for testing - reads from write model database).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllStudents()
    {
        var students = await _dbContext.Students
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        _logger.LogInformation("GetAllStudents returned {Count} students", students.Count);

        // Convert to DTOs with consistent property naming
        var studentDtos = students.ToDtoList();

        return Ok(studentDtos);
    }

    /// <summary>
    /// Get a student by ID (for testing - reads from write model database).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetStudent(int id)
    {
        var student = await _dbContext.Students.FindAsync(id);

        if (student == null)
        {
            return NotFound(new { message = "Student not found", studentId = id });
        }

        _logger.LogInformation("GetStudent returned StudentId={StudentId}", id);

        // Convert to DTO with consistent property naming
        var studentDto = student.ToDto();

        return Ok(studentDto);
    }
}
