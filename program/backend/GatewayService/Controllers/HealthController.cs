namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;

public class HealthController() : ControllerBase
{
    [Route("/manage/health")]
    [HttpGet]
    public IActionResult IsOk()
    {
        return Ok();
    }
}