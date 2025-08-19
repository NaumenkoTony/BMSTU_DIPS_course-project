using Microsoft.AspNetCore.Mvc;
using IdentityService.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace IdentityService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IAuthorizationCodeStore _codeStore;
        private readonly IClientStore _clientStore;
        private readonly ITokenService _tokenService;

        public TokenController(IAuthorizationCodeStore codeStore, IClientStore clientStore, ITokenService tokenService)
        {
            _codeStore = codeStore;
            _clientStore = clientStore;
            _tokenService = tokenService;
        }

        [HttpPost("token")]
        public async Task<IActionResult> Exchange([FromForm] string grant_type,
                                                [FromForm] string code,
                                                [FromForm] string redirect_uri,
                                                [FromForm] string client_id,
                                                [FromForm] string client_secret)
        {
            if (grant_type != "authorization_code")
                return BadRequest(new { error = "unsupported_grant_type" });

            var client = await _clientStore.FindClientByIdAsync(client_id);
            if (client == null || client.ClientSecret != client_secret)
                return Unauthorized(new { error = "invalid_client" });

            var authCode = await _codeStore.FindCodeAsync(code);
            if (authCode == null || authCode.RedirectUri != redirect_uri || authCode.Expiration < DateTime.UtcNow)
                return BadRequest(new { error = "invalid_grant" });

            var scopes = authCode.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var accessToken = await _tokenService.CreateAccessTokenAsync(authCode.UserId, "resource_server", Enumerable.Empty<Claim>(), scopes);

            var idToken = await _tokenService.CreateIdTokenAsync(authCode.UserId, client.ClientId, Enumerable.Empty<Claim>(), scopes);

            await _codeStore.RemoveCodeAsync(code);

            return Ok(new
            {
                access_token = accessToken,
                id_token = idToken,
                token_type = "Bearer",
                expires_in = 3600,
                scope = string.Join(" ", scopes)
            });
        }
    }
}
