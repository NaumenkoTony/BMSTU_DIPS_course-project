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
                IsPublic = false
            });
        }

        if (!await db.Clients.AnyAsync(c => c.ClientId == "test-client"))
        {
            db.Clients.Add(new Client
            {
                ClientId = "test-client",
                ClientSecret = "JDgvvoMQxxC7IWdpkBP8a4MkQE1KxjNTZQ0o2_8avjbfj7zIcGRyMGBReydOCZx3",
                RedirectUris = "http://localhost:8080/api/v1/authorize/callback|http://gateway_service:8080/api/v1/authorize/callback",
                AllowedScopes = "openid|profile|api",
                IsPublic = false
            });
        }

        await db.SaveChangesAsync();
    }
}
