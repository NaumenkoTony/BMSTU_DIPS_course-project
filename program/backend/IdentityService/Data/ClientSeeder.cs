namespace IdentityService.Data;

using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

public static class ClientSeeder
{
    public static async Task EnsureSeededAsync(IdentityContext db)
    {
        if (!await db.Clients.AnyAsync(c => c.ClientId == "gateway-client"))
        {
            db.Clients.Add(new Client
            {
                ClientId = "gateway-client",
                ClientSecret = "JDgvvoMQxxC7IWdpkBP8a4MkQE1KxjNTZQ0o2_8avjbfj7zIcGRyMGBReydOCZx3",
                RedirectUris = "http://localhost:8080/api/v1/authorize/callback|http://gateway_service:8080/api/v1/authorize/callback",
                AllowedScopes = "openid|profile|email",
                RequirePkce = false,
                IsPublic = false
            });
        }

        if (!await db.Clients.AnyAsync(c => c.ClientId == "locus-frontend-client"))
        {
            db.Clients.Add(new Client
            {
                ClientId = "locus-frontend-client",
                RedirectUris = "http://localhost:80/callback",
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
