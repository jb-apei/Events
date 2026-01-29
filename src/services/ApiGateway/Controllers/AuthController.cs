using ApiGateway.Models;
using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(JwtService jwtService, ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Login and receive JWT token
    /// </summary>
    /// <remarks>
    /// MVP: Simplified authentication - accepts any email/password.
    /// Production: Integrate with proper identity provider (Azure AD, Auth0, etc.)
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // MVP: Simplified auth - accept any credentials
        // TODO: Implement proper user validation against identity database
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Generate user ID from email (MVP simplification)
        var userId = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var token = _jwtService.GenerateToken(userId, request.Email);

        _logger.LogInformation("User {Email} authenticated successfully", request.Email);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = userId,
            Email = request.Email
        });
    }

    /// <summary>
    /// Validate token (optional endpoint for debugging)
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken([FromBody] string token)
    {
        var principal = _jwtService.ValidateToken(token);

        if (principal == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        return Ok(new { valid = true, userId, email });
    }
}
