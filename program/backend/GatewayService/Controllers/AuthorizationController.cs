namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

[Route("/api/v1/authorize")]
[ApiController]
public class AuthorizationController(ILogger<AuthorizationController> logger) : ControllerBase
{
    private readonly ILogger<AuthorizationController> logger = logger;

    [HttpGet("login")]
    public IActionResult Login()
    {
        logger.LogInformation("Login endpoint called");
        var redirectUri = "http://localhost:8080/api/v1/authorize/callback";
        var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        HttpContext.Session.SetString("oauth_state", state);
        var authUrl = $"http://localhost:8000/authorize" +
                    $"?response_type=code" +
                    $"&client_id=gateway-client" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    $"&scope=openid profile email" +
                    $"&state={Uri.EscapeDataString(state)}";
        logger.LogInformation("Redirect URI: {RedirectUri}", redirectUri);
        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        logger.LogInformation("Callback received. Code: {Code}", code);

        var savedState = HttpContext.Session.GetString("oauth_state");
        if (string.IsNullOrEmpty(savedState) || savedState != state)
        {
            logger.LogWarning("Invalid state parameter. Expected: {Expected}, Got: {Actual}", savedState, state);
            return BadRequest("Invalid state parameter");
        }
        HttpContext.Session.Remove("oauth_state");
        try
        {
            var tokenUrl = "http://identity_service:8000/token";
            logger.LogInformation("Sending token request to: {TokenUrl}", tokenUrl);

            var response = await new HttpClient().PostAsync(tokenUrl,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", "http://localhost:8080/api/v1/authorize/callback" },
                    { "client_id", "gateway-client" },
                    { "client_secret", "JDgvvoMQxxC7IWdpkBP8a4MkQE1KxjNTZQ0o2_8avjbfj7zIcGRyMGBReydOCZx3" }
                }));

            logger.LogInformation("Token response status: {StatusCode}", response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Token response: {Content}", content);

            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
            var accessToken = tokenResponse.GetProperty("access_token").GetString();

            return Ok(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 3600,
                scope = "openid profile email"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during token request");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("directlogin")]
    public IActionResult GetTokenDirectly([FromBody] DirectLoginRequest request)
    {
        logger.LogInformation("directlogin endpoint called");
        var redirectUri = "http://localhost:8080/api/v1/authorize/callback";
        var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        HttpContext.Session.SetString("oauth_state", state);
        var authUrl = $"http://localhost:8000/directauthorize" +
                    $"?response_type=code" +
                    $"&client_id=gateway-client" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    $"&scope=openid profile email" +
                    $"&state={Uri.EscapeDataString(state)}" +
                    $"&login={request.Username}" +
                    $"&password={request.Password}";
        logger.LogInformation("Redirect URI: {RedirectUri}", redirectUri);
        return Redirect(authUrl);
    }
}

public class DirectLoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}
