namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Contracts.Dto;
using System.Text;

[Route("/api/v1")]
[ApiController]
[Authorize]
public class AdminController(IMemoryCache memoryCache, IHttpClientFactory httpClientFactory, ILogger<AdminController> logger) : ControllerBase
{
    private readonly ILogger<AdminController> _logger = logger;
    private readonly IMemoryCache memoryCache = memoryCache;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser()
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        Console.WriteLine("Gateway received body:");
        Console.WriteLine(rawBody);

        var content = new StringContent(rawBody, Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient("IdentityService");

        Console.WriteLine("Sending body to IdentityService:");
        Console.WriteLine(await content.ReadAsStringAsync());

        var response = await client.PostAsync("admin/create-user", content);

        var responseContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"IdentityService response: {response.StatusCode}");
        Console.WriteLine(responseContent);

        return StatusCode((int)response.StatusCode, responseContent);
    }


    [HttpGet("users/roles")]
    public async Task<IActionResult> GetAvailableRoles()
    {
        _logger.LogInformation("Proxying get-roles request to IdentityService");

        try
        {
            var identityService = httpClientFactory.CreateClient("IdentityService");
            
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                return Unauthorized(new { error = "Authorization header is required" });
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "admin/users/roles");
            requestMessage.Headers.Add("Authorization", authHeader);

            var response = await identityService.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Content(responseContent, "application/json");
            }
            else
            {
                return StatusCode((int)response.StatusCode, responseContent);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to IdentityService");
            return StatusCode(503, new { error = "Identity service is unavailable" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in get-roles proxy");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}