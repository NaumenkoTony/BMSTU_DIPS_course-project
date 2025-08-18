using Microsoft.AspNetCore.Mvc;
using IdentityService.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using IdentityService.Models;

namespace IdentityService.Controllers
{
    [Route("[controller]")]
    public class AuthorizationController : Controller
    {
        private readonly IClientStore _clientStore;
        private readonly IAuthorizationCodeStore _codeStore;
        private readonly UserManager<User> _userManager;

        public AuthorizationController(IClientStore clientStore, IAuthorizationCodeStore codeStore, UserManager<User> userManager)
        {
            _clientStore = clientStore;
            _codeStore = codeStore;
            _userManager = userManager;
        }

        [HttpGet("authorize")]
        public IActionResult Authorize(
            [FromQuery] string response_type,
            [FromQuery] string client_id,
            [FromQuery] string redirect_uri,
            [FromQuery] string scope,
            [FromQuery] string state)
        {
            // Проверка client_id и redirect_uri
            var client = _clientStore.FindClient(client_id);
            if (client == null || client.RedirectUri != redirect_uri)
                return BadRequest("Invalid client or redirect_uri");

            // В данном примере - показываем страницу логина
            // Если пользователь уже залогинен — можно сразу перейти к выдаче кода
            if (!User.Identity.IsAuthenticated)
            {
                // Сохраняем параметры запроса в TempData для POST login
                TempData["response_type"] = response_type;
                TempData["client_id"] = client_id;
                TempData["redirect_uri"] = redirect_uri;
                TempData["scope"] = scope;
                TempData["state"] = state;

                return RedirectToAction("Login", "Account");
            }

            // Если пользователь залогинен, выдаём код авторизации
            return IssueAuthorizationCode(User, client, redirect_uri, scope, state);
        }

        private IActionResult IssueAuthorizationCode(ClaimsPrincipal user, Client client, string redirectUri, string scope, string state)
        {
            var code = Guid.NewGuid().ToString("N");

            var authCode = new AuthorizationCode
            {
                Code = code,
                ClientId = client.ClientId,
                UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "",
                RedirectUri = redirectUri,
                Scopes = scope,
                Expiration = DateTime.UtcNow.AddMinutes(5)
            };

            _codeStore.SaveCodeAsync(authCode).Wait();

            var uriBuilder = new UriBuilder(redirectUri);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["code"] = code;
            if (!string.IsNullOrEmpty(state))
                query["state"] = state;
            uriBuilder.Query = query.ToString();

            return Redirect(uriBuilder.ToString());
        }
    }
}
