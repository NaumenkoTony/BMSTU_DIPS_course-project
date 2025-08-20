using IdentityService.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Data;

public static class IdentitySeeder
{
    public static async Task SeedRolesAndAdminAsync(UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new Role { Name = "Admin" });
        }

        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new Role { Name = "User" });
        }

        var adminEmail = "admin@local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var admin = new User
            {
                UserName = "admin",
                Email = adminEmail,
                FirstName = "Tony",
                LastName = "Naumenko",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"[Seeder ERROR] {error.Code}: {error.Description}");
                }
            }

        }
    }
}
