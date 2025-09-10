using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IdentityService.Models;
using Microsoft.Extensions.Logging;

namespace IdentityService.Controllers;

[ApiController]
[Route("idp/connect/userinfo")]
public class UserInfoController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UserInfoController> _logger;

    public UserInfoController(UserManager<User> userManager, ILogger<UserInfoController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("UserInfo request received from user: {UserName}", 
            User.Identity?.Name);

        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found in database for authenticated principal: {UserName}", 
                    User.Identity?.Name);
                return Unauthorized();
            }

            _logger.LogInformation("Returning user info for: {UserId} ({Email})", 
                user.Id, user.Email);

            return Ok(new
            {
                sub = user.Id,
                email = user.Email,
                given_name = user.FirstName,
                family_name = user.LastName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info for: {UserName}", 
                User.Identity?.Name);
            return StatusCode(500, "Internal server error");
        }
    }
}