using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using IdentityService.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace IdentityService.Controllers
{
    [Route("/idp")]
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("account/login")]
        public IActionResult Login() => View();

        [HttpPost("account/login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            _logger.LogInformation("Login attempt for user: {Username}", username);

            var user = await _userManager.FindByNameAsync(username);
            if (user is null)
            {
                _logger.LogWarning("Login failed - user not found: {Username}", username);
                ModelState.AddModelError("", "Invalid username or password");
                return View();
            }

            _logger.LogDebug("User found: {UserId}", user.Id);

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed - invalid password for user: {Username}", username);
                ModelState.AddModelError("", "Invalid username or password");
                return View();
            }

            _logger.LogInformation("Login successful for user: {Username}", username);

            var response_type = TempData["response_type"]?.ToString() ?? "code";
            var client_id = TempData["client_id"]?.ToString() ?? "";
            var redirect_uri = TempData["redirect_uri"]?.ToString() ?? "";
            var scope = TempData["scope"]?.ToString() ?? "openid";
            var state = TempData["state"]?.ToString() ?? "";
            var code_challenge = TempData["code_challenge"]?.ToString() ?? "";
            var code_challenge_method = TempData["code_challenge_method"]?.ToString() ?? "";

            return Redirect($"/idp/authorize?response_type={Uri.EscapeDataString(response_type)}&client_id={Uri.EscapeDataString(client_id)}&redirect_uri={Uri.EscapeDataString(redirect_uri)}&scope={Uri.EscapeDataString(scope)}&state={Uri.EscapeDataString(state)}&code_challenge={Uri.EscapeDataString(code_challenge)}&code_challenge_method={Uri.EscapeDataString(code_challenge_method)}");
        }

        [HttpPost("account/logout")]
        public async Task<IActionResult> Logout()
        {
            const string methodName = nameof(Logout);
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["Method"] = methodName
            });

            _logger.LogInformation("Logout requested");

            var userName = User.Identity?.Name;
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User logged out: {UserName}", userName);
            return RedirectToAction("Login");
        }
    }
}
