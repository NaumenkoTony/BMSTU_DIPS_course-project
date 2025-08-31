namespace LoyaltyService.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Serilog;

public interface ITokenService
{
    string GetAccessToken();
    string GetUsernameFromJWT();
}

public class TokenService(IHttpContextAccessor httpContextAccessor) : ITokenService
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public string GetAccessToken()
    {
        var authorizationHeader = httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();

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
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(GetAccessToken());
            
            var username = jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "nickname")?.Value
                        ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                username = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                Log.Warning("Username claim not found, using subject: {Subject}", username);
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new UnauthorizedAccessException("Username claim not found in token.");
            }

            return username;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error extracting username from token");
            throw new UnauthorizedAccessException("Invalid token or unable to extract username.", ex);
        }
    }
}