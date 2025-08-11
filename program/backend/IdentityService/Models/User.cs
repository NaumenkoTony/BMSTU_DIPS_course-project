
namespace IdentityService.Models;

using Microsoft.AspNetCore.Identity;

public class User : IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}