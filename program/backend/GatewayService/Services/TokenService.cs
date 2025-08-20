namespace GatewayService.TokenService;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

public interface ITokenService
{
    string GetAccessToken();
    string GetUsernameFromJWT();
}

public class TokenService : ITokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IHttpContextAccessor httpContextAccessor, ILogger<TokenService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GetAccessToken()
    {
        if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("access_token", out var cookieToken) == true)
        {
            return cookieToken;
        }

        var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authorizationHeader))
        {
            throw new UnauthorizedAccessException("Authorization header is missing.");
        }
        
        var token = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader.Substring("Bearer ".Length).Trim()
            : null;

        if (string.IsNullOrEmpty(token))
        {
            throw new UnauthorizedAccessException("Token is missing or invalid in the Authorization header.");
        }

        return token;
    }

    public string GetUsernameFromJWT()
    {
        try
        {
            var token = GetAccessToken();
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                username = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                _logger.LogWarning("Using subject as username: {Subject}", username);
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new UnauthorizedAccessException("Username claim not found in token.");
            }

            _logger.LogInformation("Extracted username: {Username}", username);
            return username;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting username from token");
            throw new UnauthorizedAccessException("Invalid token or unable to extract username.", ex);
        }
    }
}