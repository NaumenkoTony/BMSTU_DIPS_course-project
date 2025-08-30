using Microsoft.AspNetCore.Mvc;
using IdentityService.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using IdentityService.Models;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace IdentityService.Controllers
{
    [Route("")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IAuthorizationCodeStore _codeStore;
        private readonly IClientStore _clientStore;
        private readonly ITokenService _tokenService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TokenController> _logger;

        public TokenController(IAuthorizationCodeStore codeStore,
                             IClientStore clientStore,
                             ITokenService tokenService,
                             UserManager<User> userManager,
                             ILogger<TokenController> logger)
        {
            _codeStore = codeStore;
            _clientStore = clientStore;
            _tokenService = tokenService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("token")]
        public async Task<IActionResult> Exchange([FromForm] string grant_type,
                                                [FromForm] string code,
                                                [FromForm] string redirect_uri,
                                                [FromForm] string client_id,
                                                [FromForm] string? client_secret,
                                                [FromForm] string? code_verifier)
        {
            _logger.LogInformation("Token exchange request received. Grant type: {GrantType}, Client: {ClientId}",
                grant_type, client_id);

            if (grant_type != "authorization_code")
            {
                _logger.LogWarning("Unsupported grant type: {GrantType}", grant_type);
                return BadRequest(new { error = "unsupported_grant_type" });
            }

            _logger.LogDebug("Looking up client: {ClientId}", client_id);
            var client = await _clientStore.FindClientByIdAsync(client_id);
            if (client == null)
            {
                _logger.LogWarning("Client not found: {ClientId}", client_id);
                return Unauthorized(new { error = "invalid_client" });
            }

            _logger.LogDebug("Looking up authorization code: {Code}", code);
            var authCode = await _codeStore.FindCodeAsync(code);
            if (authCode == null)
            {
                _logger.LogWarning("Authorization code not found: {Code}", code);
                return BadRequest(new { error = "invalid_grant" });
            }
            
            // Client secret validation
            if (!string.IsNullOrEmpty(client.ClientSecret))
            {
                if (client.ClientSecret != client_secret)
                {
                    _logger.LogWarning("Invalid client secret for client: {ClientId}", client_id);
                    return Unauthorized(new { error = "invalid_client" });
                }
                _logger.LogDebug("Client secret validation successful");
            }

            // PKCE validation
            if (client.RequirePkce)
            {
                _logger.LogDebug("PKCE required for client: {ClientId}", client_id);
                
                if (string.IsNullOrEmpty(authCode.CodeChallenge))
                {
                    _logger.LogWarning("Missing PKCE challenge for public client: {ClientId}", client_id);
                    return BadRequest(new { error = "invalid_request", error_description = "Missing PKCE for public client" });
                }

                if (string.IsNullOrEmpty(code_verifier))
                {
                    _logger.LogWarning("Missing code_verifier for PKCE: {ClientId}", client_id);
                    return BadRequest(new { error = "invalid_request", error_description = "Missing code_verifier" });
                }

                string computedChallenge;
                if (authCode.CodeChallengeMethod == "S256")
                {
                    _logger.LogDebug("Using S256 PKCE method");
                    using var sha256 = SHA256.Create();
                    var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(code_verifier));
                    computedChallenge = Base64UrlEncode(bytes);
                }
                else
                {
                    _logger.LogDebug("Using plain PKCE method");
                    computedChallenge = code_verifier;
                }

                if (computedChallenge != authCode.CodeChallenge)
                {
                    _logger.LogWarning("PKCE verification failed for client: {ClientId}", client_id);
                    return BadRequest(new { error = "invalid_grant", error_description = "PKCE verification failed" });
                }

                _logger.LogDebug("PKCE validation successful");
            }

            _logger.LogDebug("Client validation successful: {ClientId}", client_id);

            if (authCode.RedirectUri != redirect_uri)
            {
                _logger.LogWarning("Redirect URI mismatch. Expected: {Expected}, Received: {Received}",
                    authCode.RedirectUri, redirect_uri);
                return BadRequest(new { error = "invalid_grant" });
            }

            if (authCode.Expiration < DateTime.UtcNow)
            {
                _logger.LogWarning("Authorization code expired. Code: {Code}, Expiration: {Expiration}",
                    code, authCode.Expiration);
                return BadRequest(new { error = "invalid_grant" });
            }

            _logger.LogInformation("Authorization code validated successfully for user: {UserId}", authCode.UserId);

            var user = await _userManager.FindByIdAsync(authCode.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", authCode.UserId);
                return BadRequest(new { error = "invalid_grant", error_description = "User not found" });
            }

            var scopes = authCode.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            _logger.LogDebug("Requested scopes: {Scopes}", string.Join(", ", scopes));

            try
            {
                var customClaims = new[]
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("preferred_username", user.UserName),
                    new Claim("name", $"{user.FirstName} {user.LastName}".Trim())
                }.Where(c => !string.IsNullOrEmpty(c.Value)).ToList();

                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    customClaims.Add(new Claim(ClaimTypes.Role, role));
                }

                _logger.LogDebug("Creating access token for user: {UserId} with {ClaimCount} claims and {RoleCount} roles",
                    authCode.UserId, customClaims.Count, roles.Count);

                var accessToken = await _tokenService.CreateAccessTokenAsync(
                    authCode.UserId,
                    "resource_server",
                    customClaims,
                    scopes);

                _logger.LogDebug("Creating ID token for user: {UserId}", authCode.UserId);
                var idToken = await _tokenService.CreateIdTokenAsync(
                    authCode.UserId,
                    client.ClientId,
                    customClaims,
                    scopes);

                _logger.LogDebug("Removing used authorization code: {Code}", code);
                await _codeStore.RemoveCodeAsync(code);

                _logger.LogInformation("Token exchange successful for user: {UserName} ({Email}). Scopes: {Scopes}",
                    user.UserName, user.Email, string.Join(", ", scopes));

                return Ok(new
                {
                    access_token = accessToken,
                    id_token = idToken,
                    token_type = "Bearer",
                    expires_in = 3600,
                    scope = string.Join(" ", scopes)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token creation for user: {UserId}", authCode.UserId);
                return StatusCode(500, new { error = "server_error", error_description = "Internal server error during token creation" });
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}