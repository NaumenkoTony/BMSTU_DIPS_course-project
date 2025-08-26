using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using IdentityService.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace IdentityService.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager)
        {
            _signInManager = signInManager;
            _userManager   = userManager;
        }

        [HttpGet("account/login")]
        public IActionResult Login() => View();

        [HttpPost("account/login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user is null)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Invalid username or password");
                return View();
            }

            var response_type = TempData["response_type"]?.ToString() ?? "code";
            var client_id = TempData["client_id"]?.ToString() ?? "";
            var redirect_uri = TempData["redirect_uri"]?.ToString() ?? "";
            var scope = TempData["scope"]?.ToString() ?? "openid";
            var state = TempData["state"]?.ToString() ?? "";
            var code_challenge = TempData["code_challenge"]?.ToString() ?? "";
            var code_challenge_method = TempData["code_challenge_method"]?.ToString() ?? "";

            return Redirect($"/authorize?response_type={Uri.EscapeDataString(response_type)}&client_id={Uri.EscapeDataString(client_id)}&redirect_uri={Uri.EscapeDataString(redirect_uri)}&scope={Uri.EscapeDataString(scope)}&state={Uri.EscapeDataString(state)}&code_challenge={Uri.EscapeDataString(code_challenge)}&code_challenge_method={Uri.EscapeDataString(code_challenge_method)}");
        }

        [HttpPost("account/logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
