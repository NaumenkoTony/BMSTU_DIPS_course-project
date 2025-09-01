namespace IdentityService.Data;

using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

public static class ClientSeeder
{
    public static async Task EnsureSeededAsync(IdentityContext db, IConfiguration config)
    {
        var authSection = config.GetSection("Authentication");

        var backendClient = authSection.GetSection("BackendClient");
        var frontendClient = authSection.GetSection("FrontendClient");

        if (!await db.Clients.AnyAsync(c => c.ClientId == backendClient["ClientId"]))
        {
            db.Clients.Add(new Client
            {
                ClientId = backendClient["ClientId"]!,
                Audience = "locus_app",
                ClientSecret = backendClient["ClientSecret"],
                RedirectUris = backendClient["RedirectUri"]!,
                AllowedScopes = "openid|profile|email",
                RequirePkce = false,
                IsPublic = false
            });
        }

        if (!await db.Clients.AnyAsync(c => c.ClientId == frontendClient["ClientId"]))
        {
            db.Clients.Add(new Client
            {
                ClientId = frontendClient["ClientId"]!,
                Audience = "locus_app",
                RedirectUris = string.Join("|", frontendClient.GetSection("RedirectUris").Get<string[]>()!),
                AllowedScopes = "openid|profile|email",
                RequirePkce = true,
                IsPublic = true
            });
        }

        if (!await db.Clients.AnyAsync(c => c.ClientId == "test-client"))
        {
            db.Clients.Add(new Client
            {
                ClientId = "test-client",
                Audience = "locus_app",
                ClientSecret = "96fe1ef451dec6af6d87e58d09372fee837092f52c1b8da24213d6c972c4f7c1",
                RedirectUris = "http://localhost:8080/api/v1/authorize/callback|http://gateway_service:8080/api/v1/authorize/callback",
                AllowedScopes = "openid|profile|api",
                RequirePkce = false,
                IsPublic = false
            });
        }

        await db.SaveChangesAsync();
    }
}
