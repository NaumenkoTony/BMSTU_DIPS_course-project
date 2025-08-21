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

        if (!await roleManager.RoleExistsAsync("TestUser"))
        {
            await roleManager.CreateAsync(new Role { Name = "TestUser" });
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

        var testUserEmail = "test@local";
        var testUser = await userManager.FindByEmailAsync(testUserEmail);
        if (testUser == null)
        {
            var test = new User
            {
                UserName = "Test",
                Email = testUserEmail,
                FirstName = "test",
                LastName = "test",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(test, "Test123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(test, "TestUser");
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
