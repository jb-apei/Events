using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstructorsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InstructorsController> _logger;

    public InstructorsController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<InstructorsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get all instructors (proxied to InstructorService).
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetInstructors()
    {
        var instructorServiceUrl = _configuration["InstructorService:Url"] ?? "http://localhost:5130";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{instructorServiceUrl}/api/instructors");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            else
            {
                _logger.LogWarning("Failed to fetch instructors from service: {StatusCode}", response.StatusCode);
                return Ok(Array.Empty<object>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching instructors from service");
            return Ok(Array.Empty<object>());
        }
    }

    /// <summary>
    /// Get instructor by ID (proxied to InstructorService).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetInstructor(int id)
    {
        var instructorServiceUrl = _configuration["InstructorService:Url"] ?? "http://localhost:5130";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{instructorServiceUrl}/api/instructors/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = "Instructor not found", instructorId = id });
            }
            else
            {
                _logger.LogWarning("Failed to fetch instructor {Id} from service: {StatusCode}", id, response.StatusCode);
                return StatusCode(500, new { message = "Error fetching instructor" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching instructor {Id} from service", id);
            return StatusCode(500, new { message = "Error fetching instructor" });
        }
    }

    /// <summary>
    /// Create a new instructor (forwarded to InstructorService).
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateInstructor([FromBody] object command)
    {
        var instructorServiceUrl = _configuration["InstructorService:Url"] ?? "http://localhost:5130";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(command),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync($"{instructorServiceUrl}/api/instructors", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Content(responseContent, "application/json");
            }
            else
            {
                return StatusCode((int)response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating instructor");
            return StatusCode(500, new { message = "Error creating instructor" });
        }
    }

    /// <summary>
    /// Update an existing instructor (forwarded to InstructorService).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateInstructor(int id, [FromBody] object command)
    {
        var instructorServiceUrl = _configuration["InstructorService:Url"] ?? "http://localhost:5130";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(command),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await httpClient.PutAsync($"{instructorServiceUrl}/api/instructors/{id}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Content(responseContent, "application/json");
            }
            else
            {
                return StatusCode((int)response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating instructor {Id}", id);
            return StatusCode(500, new { message = "Error updating instructor" });
        }
    }
}
