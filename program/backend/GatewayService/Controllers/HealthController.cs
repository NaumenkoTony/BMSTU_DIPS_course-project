namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[Route("/api/v1")]
[ApiController]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

<<<<<<< HEAD
    [HttpGet("/manage/health")]
=======
    [Route("manage/health")]
    [HttpGet]
>>>>>>> v3
    public IActionResult IsOk()
    {
        const string methodName = nameof(IsOk);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Endpoint"] = "GET /api/v1/manage/health"
        });

        _logger.LogInformation("Health check endpoint called");
        
        try
        {        
            _logger.LogInformation("Health check completed successfully");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, "Service unhealthy");
        }
    }
}