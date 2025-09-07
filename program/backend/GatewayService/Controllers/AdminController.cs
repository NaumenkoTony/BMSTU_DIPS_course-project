namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Contracts.Dto;
using System.Text;
using GatewayService.Services;
using StackExchange.Redis;
using GatewayService.TokenService;
using Microsoft.Extensions.Logging;

[Route("/api/v1")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdminController> _logger;
    private readonly CircuitBreaker _loyaltyServiceCircuitBreaker;
    private readonly IConnectionMultiplexer _redis;
    private readonly ITokenService _tokenService;

    public AdminController(
        IMemoryCache memoryCache,
        IHttpClientFactory httpClientFactory,
        ILogger<AdminController> logger,
        IConnectionMultiplexer redis,
        ITokenService tokenService)
    {
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _redis = redis;
        _tokenService = tokenService;
        _loyaltyServiceCircuitBreaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser()
    {
        const string methodName = nameof(CreateUser);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Endpoint"] = "POST /api/v1/create-user"
        });

        _logger.LogInformation("Starting user creation process");

        try
        {
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            _logger.LogDebug("Received request body: {RequestBody}", rawBody);

            var content = new StringContent(rawBody, Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("IdentityService");

            _logger.LogInformation("Forwarding user creation to IdentityService");

            var response = await client.PostAsync("/idp/admin/create-user", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("IdentityService responded with status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IdentityService returned error: {StatusCode} - {ResponseContent}",
                    response.StatusCode, responseContent);
                return StatusCode((int)response.StatusCode, responseContent);
            }

            _logger.LogInformation("User successfully created in IdentityService");

            var accessToken = _tokenService.GetAccessToken();

            try
            {
                var user = JsonSerializer.Deserialize<JsonElement>(rawBody);
                var username = user.GetProperty("username").GetString();

                _logger.LogInformation("Creating loyalty account for user: {Username}", username);

                var loyaltyJson = $"\"{username}\"";
                var loyaltyContent = new StringContent(loyaltyJson, Encoding.UTF8, "application/json");
                var loyaltyClient = _httpClientFactory.CreateClient("LoyaltyService");

                var loyaltyResponse = await loyaltyClient.PostAsync("/api/v1/loyalties/create-user", loyaltyContent);
                var loyaltyResponseContent = await loyaltyResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("LoyaltyService responded with status: {StatusCode}",
                    loyaltyResponse.StatusCode);

                if (!loyaltyResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("LoyaltyService returned error: {StatusCode} - {ResponseContent}",
                        loyaltyResponse.StatusCode, loyaltyResponseContent);

                    _loyaltyServiceCircuitBreaker.RecordFailure();

                    _logger.LogWarning("Circuit breaker state updated. Failures recorded");

                    return StatusCode(503, new { message = "Loyalty Service unavailable" });
                }

                _logger.LogInformation("Loyalty account successfully created for user: {Username}", username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling LoyaltyService for user creation");

                await EnqueueLoyaltyRequestAsync(accessToken);

                _logger.LogInformation("Loyalty request queued for retry");
            }

            _logger.LogInformation("User creation process completed successfully");
            return StatusCode((int)response.StatusCode, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in user creation process");
            return StatusCode(500, new { error = "Internal server error during user creation" });
        }
    }

    [HttpGet("users/roles")]
    public async Task<IActionResult> GetAvailableRoles()
    {
        const string methodName = nameof(GetAvailableRoles);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Endpoint"] = "GET /api/v1/users/roles"
        });

        _logger.LogInformation("Retrieving available roles");

        try
        {
            var identityService = _httpClientFactory.CreateClient("IdentityService");

            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("Authorization header is missing");
                return Unauthorized(new { error = "Authorization header is required" });
            }

            _logger.LogDebug("Forwarding roles request to IdentityService");

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/idp/admin/users/roles");
            requestMessage.Headers.Add("Authorization", authHeader);

            var response = await identityService.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("IdentityService roles response: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully retrieved available roles");
                return Content(responseContent, "application/json");
            }
            else
            {
                _logger.LogWarning("IdentityService returned error for roles: {StatusCode} - {ResponseContent}",
                    response.StatusCode, responseContent);
                return StatusCode((int)response.StatusCode, responseContent);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to IdentityService for roles retrieval");
            return StatusCode(503, new { error = "Identity service is unavailable" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in roles retrieval");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task EnqueueLoyaltyRequestAsync(string accessToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.ListRightPushAsync("loyalty-queue", accessToken);

            _logger.LogInformation("Loyalty request queued successfully. Queue: loyalty-queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue loyalty request");
            throw;
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var client = _httpClientFactory.CreateClient("StatisticsService");
        var response = await client.GetAsync("/api/v1/statistics/summary");
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? username = null)
    {
        var client = _httpClientFactory.CreateClient("StatisticsService");

        var url = $"/api/v1/statistics/recent?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(username))
            url += $"&username={Uri.EscapeDataString(username)}";

        var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

}