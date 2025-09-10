using Microsoft.AspNetCore.Mvc;
using IdentityService.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.Web;
using IdentityService.Models;

namespace IdentityService.Controllers
{
    [Route("idp")]
    [ApiController]
    public class AuthorizationController : Controller
    {
        private readonly IClientStore _clientStore;
        private readonly IAuthorizationCodeStore _codeStore;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthorizationController> _logger;

        public AuthorizationController(IClientStore clientStore, IAuthorizationCodeStore codeStore, 
            UserManager<User> userManager, SignInManager<User> signInManager, ILogger<AuthorizationController> logger)
        {
            _clientStore = clientStore;
            _codeStore = codeStore;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize(
            [FromQuery] string response_type,
            [FromQuery] string client_id,
            [FromQuery] string redirect_uri,
            [FromQuery] string scope,
            [FromQuery] string state,
            [FromQuery] string code_challenge,
            [FromQuery] string code_challenge_method = "plain")
        {
            _logger.LogInformation("Authorize request: client_id={ClientId}, redirect_uri={RedirectUri}", 
                client_id, redirect_uri);

            var client = await _clientStore.FindClientByIdAsync(client_id);
            if (client == null)
            {
                _logger.LogWarning("Unknown client: {ClientId}", client_id);
                return BadRequest("Unknown client id");
            }

            if (!_clientStore.ValidateRedirectUri(client, redirect_uri))
            {
                _logger.LogWarning("Invalid redirect_uri. Client: {ClientRedirectUris}, Request: {RequestRedirectUri}", 
                    client.RedirectUris, redirect_uri);
                return BadRequest("Invalid redirect_uri");
            }

            var requestedScopes = (scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!_clientStore.ValidateScopes(client, requestedScopes))
            {
                _logger.LogWarning("Invalid scopes: {Scopes}", scope);
                return BadRequest("Invalid or unauthorized scopes");
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                _logger.LogInformation("User not authenticated, redirecting to login");
                TempData["response_type"] = response_type;
                TempData["client_id"] = client_id;
                TempData["redirect_uri"] = redirect_uri;
                TempData["scope"] = scope;
                TempData["state"] = state;
                TempData["code_challenge"] = code_challenge;
                TempData["code_challenge_method"] = code_challenge_method;

                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("User already authenticated, issuing authorization code");
            return await IssueAuthorizationCodeAsync(User, client, redirect_uri, requestedScopes, state, code_challenge, code_challenge_method);
        }

        [HttpGet("directauthorize")]
        public async Task<IActionResult> DirectAuthorize(
            [FromQuery] string response_type,
            [FromQuery] string client_id,
            [FromQuery] string redirect_uri,
            [FromQuery] string scope,
            [FromQuery] string state,
            [FromQuery] string login,
            [FromQuery] string password)
        {
            _logger.LogInformation("DirectAuthorize request: client_id={ClientId}, login={Login}", 
                client_id, login);

            var client = await _clientStore.FindClientByIdAsync(client_id);
            if (client == null)
            {
                _logger.LogWarning("Unknown client: {ClientId}", client_id);
                return BadRequest("Unknown client id");
            }

            if (!_clientStore.ValidateRedirectUri(client, redirect_uri))
            {
                _logger.LogWarning("Invalid redirect_uri: {RedirectUri}", redirect_uri);
                return BadRequest("Invalid redirect_uri");
            }

            var requestedScopes = (scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!_clientStore.ValidateScopes(client, requestedScopes))
            {
                _logger.LogWarning("Invalid scopes: {Scopes}", scope);
                return BadRequest("Invalid or unauthorized scopes");
            }

            var user = await _userManager.FindByNameAsync(login);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Login}", login);
                return Unauthorized("Invalid username or password");
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Invalid password for user: {Login}", login);
                return Unauthorized("Invalid username or password");
            }

            _logger.LogInformation("Direct authentication successful for user: {Login}", login);
            return await IssueAuthorizationCodeAsync(User, client, redirect_uri, requestedScopes, state);
        }

        private async Task<IActionResult> IssueAuthorizationCodeAsync(
            ClaimsPrincipal user,
            Client client,
            string redirectUri,
            string[] scopes,
            string state,
            string? codeChallenge = null,
            string? codeChallengeMethod = null)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("Authenticated principal has no NameIdentifier");
                return Forbid("Authenticated principal has no NameIdentifier.");
            }

            var code = Guid.NewGuid().ToString("N");

            _logger.LogDebug("Creating authorization code: Challenge={Challenge}, Method={Method}", 
                codeChallenge, codeChallengeMethod);
            
            var authCode = new AuthorizationCode
            {
                Code = code,
                ClientId = client.ClientId,
                UserId = userId,
                RedirectUri = redirectUri,
                Scopes = string.Join(' ', scopes),
                Expiration = DateTime.UtcNow.AddMinutes(5),
                CodeChallenge = codeChallenge,
                CodeChallengeMethod = codeChallengeMethod
            };

            await _codeStore.SaveCodeAsync(authCode);

            var uriBuilder = new UriBuilder(redirectUri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["code"] = code;
            if (!string.IsNullOrEmpty(state))
                query["state"] = state;
            uriBuilder.Query = query.ToString();

            _logger.LogInformation("Authorization code issued for user {UserId}, redirecting to {RedirectUri}", 
                userId, uriBuilder.ToString());

            return Redirect(uriBuilder.ToString());
        }
    }
}