using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


[ApiController]
[Route("idp/.well-known")]
public class JwksController : ControllerBase
{
    private readonly IJwksService _jwksService;
    private readonly ILogger<JwksController> _logger;

    public JwksController(IJwksService jwksService, ILogger<JwksController> logger)
    {
        _jwksService = jwksService;
        _logger = logger;
    }

    [HttpGet("jwks.json")]
    public IActionResult GetJwks()
    {
        _logger.LogInformation("JWKS request from {RemoteIpAddress}. User-Agent: {UserAgent}", 
            HttpContext.Connection.RemoteIpAddress, 
            Request.Headers.UserAgent.ToString());
        
        _logger.LogDebug("JWKS requested at {Timestamp}", DateTime.UtcNow);
        
        try
        {
            var keys = _jwksService.GetJwks();
            
            _logger.LogInformation("JWKS returned {KeyCount} keys. Key IDs: {KeyIds}", 
                keys.Keys?.Count ?? 0,
                string.Join(", ", keys.Keys?.Select(k => k.Kid) ?? Array.Empty<string>()));
            
            return Ok(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWKS");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("openid-configuration")]
    public IActionResult GetConfiguration()
    {
        _logger.LogInformation("OpenID configuration request from {RemoteIpAddress}. User-Agent: {UserAgent}", 
            HttpContext.Connection.RemoteIpAddress,
            Request.Headers.UserAgent.ToString());

        try
        {
            var issuer = $"{Request.Scheme}://{Request.Host}/idp";
            _logger.LogDebug("Using issuer: {Issuer}", issuer);

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

            _logger.LogInformation("OpenID configuration returned for issuer {Issuer}", issuer);
            
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating OpenID configuration");
            return StatusCode(500, "Internal server error");
        }
    }
}