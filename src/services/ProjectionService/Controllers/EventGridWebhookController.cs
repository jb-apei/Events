using Microsoft.AspNetCore.Mvc;
using ProjectionService.Services;

namespace ProjectionService.Controllers;

/// <summary>
/// Webhook endpoint for Azure Event Grid push subscriptions.
/// Receives events from Event Grid topics and routes them to handlers.
/// </summary>
[ApiController]
[Route("events")]
public class EventGridWebhookController : ControllerBase
{
    private readonly EventDispatcher _eventDispatcher;
    private readonly ILogger<EventGridWebhookController> _logger;

    public EventGridWebhookController(
        EventDispatcher eventDispatcher,
        ILogger<EventGridWebhookController> logger)
    {
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint for Event Grid push subscriptions.
    /// Handles subscription validation and event processing.
    /// </summary>
    /// <returns>
    /// - 200 OK with validation response for subscription validation
    /// - 200 OK for successfully processed events
    /// - 500 Internal Server Error for processing failures (triggers Event Grid retry)
    /// </returns>
    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveEvents(CancellationToken cancellationToken)
    {
        try
        {
            // Read raw request body
            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);

            _logger.LogInformation("Received Event Grid webhook request. Size: {Size} bytes", requestBody.Length);

            // Process events and handle validation
            var validationResponse = await _eventDispatcher.ProcessEventGridWebhookAsync(
                requestBody,
                cancellationToken);

            // Return validation response if this was a subscription validation request
            if (validationResponse != null)
            {
                _logger.LogInformation("Returning validation response: {ValidationCode}",
                    validationResponse.ValidationResponse);
                return Ok(validationResponse);
            }

            _logger.LogInformation("Successfully processed Event Grid webhook request");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process Event Grid webhook request");

            // Return 500 to trigger Event Grid retry
            return StatusCode(500, new { error = "Event processing failed" });
        }
    }

    /// <summary>
    /// OPTIONS handler for Event Grid webhook validation.
    /// Event Grid sends OPTIONS request to validate the endpoint before creating subscription.
    /// </summary>
    [HttpOptions("webhook")]
    public IActionResult OptionsWebhook()
    {
        // Event Grid validates the endpoint by sending OPTIONS request
        // Return 200 OK with appropriate headers
        Response.Headers.Append("WebHook-Allowed-Origin", "*");
        Response.Headers.Append("WebHook-Allowed-Rate", "*");
        return Ok();
    }

    /// <summary>
    /// Health check endpoint for Event Grid subscription validation.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "ProjectionService" });
    }
}
