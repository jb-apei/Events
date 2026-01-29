using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<StudentsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get all students (proxied to StudentService).
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetStudents()
    {
        var studentServiceUrl = _configuration["StudentService:Url"] ?? "http://localhost:5120";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{studentServiceUrl}/api/students");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            else
            {
                _logger.LogWarning("Failed to fetch students from service: {StatusCode}", response.StatusCode);
                return Ok(Array.Empty<object>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching students from service");
            return Ok(Array.Empty<object>());
        }
    }

    /// <summary>
    /// Get student by ID (proxied to StudentService).
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetStudent(int id)
    {
        var studentServiceUrl = _configuration["StudentService:Url"] ?? "http://localhost:5120";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{studentServiceUrl}/api/students/{id}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound(new { message = "Student not found", studentId = id });
            }
            else
            {
                _logger.LogWarning("Failed to fetch student {Id} from service: {StatusCode}", id, response.StatusCode);
                return StatusCode(500, new { message = "Error fetching student" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching student {Id} from service", id);
            return StatusCode(500, new { message = "Error fetching student" });
        }
    }

    /// <summary>
    /// Create a new student (forwarded to StudentService).
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateStudent([FromBody] object command)
    {
        var studentServiceUrl = _configuration["StudentService:Url"] ?? "http://localhost:5120";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(command),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync($"{studentServiceUrl}/api/students", content);
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
            _logger.LogError(ex, "Error creating student");
            return StatusCode(500, new { message = "Error creating student" });
        }
    }

    /// <summary>
    /// Update an existing student (forwarded to StudentService).
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateStudent(int id, [FromBody] object command)
    {
        var studentServiceUrl = _configuration["StudentService:Url"] ?? "http://localhost:5120";
        
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(command),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await httpClient.PutAsync($"{studentServiceUrl}/api/students/{id}", content);
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
            _logger.LogError(ex, "Error updating student {Id}", id);
            return StatusCode(500, new { message = "Error updating student" });
        }
    }
}
