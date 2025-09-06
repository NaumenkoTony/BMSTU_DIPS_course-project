namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

[Route("/api/v1/authorize")]
[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AuthorizationController> _logger;
    private readonly IConfiguration _config;

    public AuthorizationController(
        IMemoryCache memoryCache,
        ILogger<AuthorizationController> logger,
        IConfiguration config)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _config = config;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        const string methodName = nameof(Login);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Endpoint"] = "GET /api/v1/authorize/login"
        });

        _logger.LogInformation("Starting OAuth login flow");

        try
        {
            var redirectUri = _config["Authentication:BackendClient:RedirectUri"];
            var clientId = _config["Authentication:BackendClient:ClientId"];
            var authority = _config["Authentication:AuthorityLocal"];

            var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            _memoryCache.Set("oauth_state", state, TimeSpan.FromMinutes(5));

            var authUrl = $"{authority}/authorize" +
                          $"?response_type=code" +
                          $"&client_id={clientId}" +
                          $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                          $"&scope=openid profile email" +
                          $"&state={Uri.EscapeDataString(state)}";

            var customHost = _config["Authentication:Host"];
            if (!string.IsNullOrEmpty(customHost))
            {
                authUrl += $"&Host={Uri.EscapeDataString(customHost)}";
            }

            return Redirect(authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login flow initialization");
            return StatusCode(500, "Internal server error during login initialization");
        }
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        const string methodName = nameof(Callback);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Endpoint"] = "GET /api/v1/authorize/callback",
            ["Code"] = code,
            ["State"] = state
        });

        _logger.LogInformation("Processing OAuth callback");

        try
        {
            if (!_memoryCache.TryGetValue("oauth_state", out string savedState))
            {
                _logger.LogWarning("State not found in cache - possible expired or missing state");
                return BadRequest("Invalid state parameter");
            }

            if (savedState != state)
            {
                _logger.LogWarning("State mismatch. Expected: {ExpectedState}, Actual: {ActualState}", 
                    savedState, state);
                return BadRequest("Invalid state parameter");
            }

            _memoryCache.Remove("oauth_state");
            _logger.LogDebug("State validation successful, removed from cache");

            var tokenUrl = $"{_config["Authentication:Authority"]}/token";
            
            _logger.LogInformation("Exchanging authorization code for token at: {TokenUrl}", tokenUrl);

            using var httpClient = new HttpClient();
            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", _config["Authentication:BackendClient:RedirectUri"] },
                { "client_id", _config["Authentication:BackendClient:ClientId"] },
                { "client_secret", _config["Authentication:BackendClient:ClientSecret"] }
            });

            var response = await httpClient.PostAsync(tokenUrl, tokenRequestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Token endpoint response: {StatusCode}", response.StatusCode);
            _logger.LogDebug("Token response content: {ResponseContent}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token endpoint returned error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);
                return StatusCode((int)response.StatusCode, responseContent);
            }

            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var accessToken = tokenResponse.GetProperty("access_token").GetString();

            _logger.LogInformation("Successfully obtained access token");

            return Ok(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 3600,
                scope = "openid profile email"
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize token response");
            return StatusCode(500, "Invalid token response format");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to token endpoint");
            return StatusCode(503, "Token service unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token exchange");
            return StatusCode(500, "Internal server error during token exchange");
        }
    }

    [HttpPost("directlogin")]
    public IActionResult GetTokenDirectly([FromBody] DirectLoginRequest request)
    {
        const string methodName = nameof(GetTokenDirectly);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Endpoint"] = "POST /api/v1/authorize/directlogin",
            ["Username"] = request.Username
        });

        _logger.LogInformation("Starting direct login flow for user: {Username}", request.Username);

        try
        {           
            var redirectUri = _config["Authentication:BackendClient:RedirectUri"];
            var clientId = _config["Authentication:BackendClient:ClientId"];
            var authority = _config["Authentication:AuthorityLocal"];

            var state = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            _memoryCache.Set("oauth_state", state, TimeSpan.FromMinutes(5));
            
            _logger.LogDebug("Generated OAuth state for direct login: {State}", state);

            var authUrl = $"{authority}/directauthorize" +
                        $"?response_type=code" +
                        $"&client_id={clientId}" +
                        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                        $"&scope=openid profile email" +
                        $"&state={Uri.EscapeDataString(state)}" +
                        $"&login={request.Username}" +
                        $"&password={request.Password}";
            var customHost = _config["Host"];
            if (!string.IsNullOrEmpty(customHost))
            {
                authUrl += $"&Host={Uri.EscapeDataString(customHost)}";
            }

            _logger.LogInformation("Redirecting to direct authorization endpoint");
            
            return Redirect(authUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during direct login flow for user: {Username}", request.Username);
            return StatusCode(500, "Internal server error during direct login");
        }
    }
}

public class DirectLoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}