namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

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
        logger.LogInformation("Callback received. Code: {Code}}", code);
        if (string.IsNullOrEmpty(code))
            return BadRequest("Authorization code is missing.");

        var tokenResponse = await new HttpClient().PostAsync("http://identity_service:8000/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", "http://localhost:8080/api/v1/authorize/callback" },
                { "client_id", "gateway-client" },
                { "client_secret", "secret" }
            }));

        var content = await tokenResponse.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }
}
