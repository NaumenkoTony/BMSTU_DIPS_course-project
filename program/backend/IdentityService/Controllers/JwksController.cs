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

    [HttpGet("openid-configuration")]
    public IActionResult GetConfiguration()
    {
        var issuer = $"{Request.Scheme}://{Request.Host}";

        var config = new
        {
            issuer,
            authorization_endpoint = $"{issuer}/authorize",
            token_endpoint = $"{issuer}/token",
            jwks_uri = $"{issuer}/.well-known/jwks.json",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code" },
            scopes_supported = new[] { "openid", "profile", "email" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        };

        return Ok(config);
    }
}
