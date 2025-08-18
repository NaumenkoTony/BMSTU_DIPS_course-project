using Microsoft.AspNetCore.Mvc;
using IdentityService.Services;
using System.Threading.Tasks;
using System.Linq;

namespace IdentityService.Controllers
{
    [Route("[controller]")]
    public class TokenController : Controller
    {
        private readonly IAuthorizationCodeStore _codeStore;
        private readonly IClientStore _clientStore;
        private readonly ITokenService _tokenService;
        private readonly UserManager<User> _userManager;

        public TokenController(
            IAuthorizationCodeStore codeStore,
            IClientStore clientStore,
            ITokenService tokenService,
            UserManager<User> userManager)
        {
            _codeStore = codeStore;
            _clientStore = clientStore;
            _tokenService = tokenService;
            _userManager = userManager;
        }

        [HttpPost("token")]
        public async Task<IActionResult> Token([FromForm] string grant_type, [FromForm] string code,
            [FromForm] string redirect_uri, [FromForm] string client_id, [FromForm] string client_secret)
        {
            if (grant_type != "authorization_code")
                return BadRequest("Unsupported grant_type");

            var client = _clientStore.FindClient(client_id);
            if (client == null || client.ClientSecret != client_secret)
                return BadRequest("Invalid client credentials");

            var authCode = await _codeStore.FindCodeAsync(code);
            if (authCode == null || authCode.ClientId != client_id || authCode.RedirectUri != redirect_uri || authCode.Expiration < DateTime.UtcNow)
                return BadRequest("Invalid or expired authorization code");

            var user = await _userManager.FindByIdAsync(authCode.UserId);
            if (user == null)
                return BadRequest("User not found");

            // Удаляем код после использования
            await _codeStore.RemoveCodeAsync(code);

            var scopes = authCode.Scopes.Split(' ');

            var claims = await _userManager.GetClaimsAsync(user);

            var idToken = await _tokenService.CreateIdTokenAsync(user.Id, client_id, claims, scopes);
            var accessToken = await _tokenService.CreateAccessTokenAsync(user.Id, client_id, claims, scopes);

            return Ok(new
            {
                id_token = idToken,
                access_token = accessToken,
                token_type = "Bearer",
                expires_in = 3600
            });
        }
    }
}
