using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IdentityService.Models;

namespace IdentityService.Controllers;

[ApiController]
[Route("connect/userinfo")]
public class UserInfoController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public UserInfoController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        return Ok(new
        {
            sub = user.Id,
            email = user.Email,
            given_name = user.FirstName,
            family_name = user.LastName
        });
    }
}
