using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route(".well-known")]
public class JwksController : ControllerBase
{
    private readonly IJwksService _jwksService;

    public JwksController(IJwksService jwksService)
    {
        _jwksService = jwksService;
    }

    [HttpGet("jwks.json")]
    public IActionResult GetJwks()
    {
        var keys = _jwksService.GetJwks();
        return Ok(keys);
    }
}
