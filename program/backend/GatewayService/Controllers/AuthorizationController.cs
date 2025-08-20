namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

[Route("/api/v1/authorize")]
[ApiController]
public class AuthorizationController(ILogger<AuthorizationController> logger, IConfiguration config) : ControllerBase
{
    private readonly ILogger<AuthorizationController> logger = logger;
    private readonly IConfiguration config = config;


    [HttpGet("login")]
    public IActionResult Login()
    {
        logger.LogInformation("Login endpoint called");
        var redirectUri = "http://localhost:8080/api/v1/authorize/callback";
        var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
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
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        logger.LogInformation("Callback received. Code: {Code}", code);
        
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
                    { "client_secret", "secret" }
                }));

            logger.LogInformation("Token response status: {StatusCode}", response.StatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Token response: {Content}", content);
            
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(content);
            var accessToken = tokenResponse.GetProperty("access_token").GetString();
            
            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                Expires = DateTimeOffset.Now.AddHours(1)
            });
            
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
}
