using System.ComponentModel.DataAnnotations;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[Authorize(Roles = "Admin", AuthenticationSchemes = "Bearer")]
[ApiController]
[Route("/idp/[controller]")]
public class AdminController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(UserManager<User> userManager, ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        _logger.LogInformation("Starting user creation for email: {Email}, username: {Username}",
            request.Email, request.UserName);

        try
        {
            _logger.LogDebug("Checking if user with email {Email} already exists", request.Email);
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("User creation failed - email already exists: {Email}", request.Email);
                return BadRequest(new { error = "Пользователь с таким email уже существует" });
            }

            _logger.LogDebug("Checking if user with username {Username} already exists", request.UserName);
            existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
            {
                _logger.LogWarning("User creation failed - username already exists: {Username}", request.UserName);
                return BadRequest(new { error = "Пользователь с таким именем уже существует" });
            }

            _logger.LogDebug("Creating new user object");
            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = true
            };

            _logger.LogInformation("Attempting to create user: {Email} with roles: {Roles}",
                request.Email, request.Roles != null ? string.Join(", ", request.Roles) : "None");

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                var errorString = string.Join(", ", errors);

                _logger.LogError("User creation failed for {Email}. Errors: {Errors}",
                    request.Email, errorString);

                return BadRequest(new { errors });
            }

            _logger.LogInformation("User created successfully with ID: {UserId}", user.Id);

            if (request.Roles != null && request.Roles.Length > 0)
            {
                _logger.LogDebug("Adding roles to user {UserId}: {Roles}",
                    user.Id, string.Join(", ", request.Roles));

                var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
                if (!roleResult.Succeeded)
                {
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to add roles to user {UserId}. Errors: {Errors}",
                        user.Id, roleErrors);
                }
                else
                {
                    _logger.LogInformation("Roles successfully added to user {UserId}", user.Id);
                }
            }

            _logger.LogInformation("User creation completed successfully: {Email} (ID: {UserId})",
                request.Email, user.Id);

            return Ok(new
            {
                message = "User created",
                userId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error creating user {Email}. Exception: {ExceptionMessage}",
                request.Email, ex.Message);

            return StatusCode(500, new
            {
                error = "Internal server error",
            });
        }
    }

    [HttpGet("users/roles")]
    public IActionResult GetAvailableRoles()
    {
        _logger.LogInformation("Retrieving available roles");
        
        try
        {
            var roles = new[] { "User", "Admin" };
            _logger.LogDebug("Available roles: {Roles}", string.Join(", ", roles));
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available roles");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class CreateUserRequest
{
    public required string UserName { get; set; }
    
    public required string Email { get; set; }
    
    public required string FirstName { get; set; }
    
    public required string LastName { get; set; }
    
    public required string Password { get; set; }
    
    public required string[] Roles { get; set; } = Array.Empty<string>();
}