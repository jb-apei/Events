using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentService.Infrastructure;

namespace StudentService.Controllers;

/// <summary>
/// Diagnostic endpoints for viewing outbox messages (development only).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly StudentDbContext _dbContext;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        StudentDbContext dbContext,
        ILogger<DiagnosticsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all outbox messages.
    /// </summary>
    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutboxMessages()
    {
        var messages = await _dbContext.Outbox
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .ToListAsync();

        return Ok(new
        {
            total = messages.Count,
            messages = messages.Select(m => new
            {
                m.Id,
                m.EventId,
                m.EventType,
                m.CreatedAt,
                m.Published,
                m.PublishedAt,
                payloadPreview = m.Payload.Length > 200 ? m.Payload.Substring(0, 200) + "..." : m.Payload
            })
        });
    }

    /// <summary>
    /// Get a specific outbox message with full payload.
    /// </summary>
    [HttpGet("outbox/{id}")]
    public async Task<IActionResult> GetOutboxMessage(long id)
    {
        var message = await _dbContext.Outbox.FindAsync(id);

        if (message == null)
        {
            return NotFound();
        }

        return Ok(message);
    }

    /// <summary>
    /// Get outbox statistics.
    /// </summary>
    [HttpGet("outbox/stats")]
    public async Task<IActionResult> GetOutboxStats()
    {
        var total = await _dbContext.Outbox.CountAsync();
        var published = await _dbContext.Outbox.CountAsync(o => o.Published);
        var pending = total - published;

        return Ok(new
        {
            total,
            published,
            pending,
            oldestPending = await _dbContext.Outbox
                .Where(o => !o.Published)
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.CreatedAt)
                .FirstOrDefaultAsync()
        });
    }
}
