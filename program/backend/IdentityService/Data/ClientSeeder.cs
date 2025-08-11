
namespace IdentityService.Data;

using IdentityService.Models;
using Microsoft.EntityFrameworkCore;


public static class ClientSeeder
{
    public static async Task EnsureSeededAsync(IHostApplicationLifetime lifetime, IdentityContext db)
    {
        if (await db.Clients.AnyAsync()) return;

        var gateway = new Client
        {
            ClientId = "gateway-client",
            ClientSecret = "e5e9fa1ba31ecd1ae84f75caaa474f3a663f05f4",
            RedirectUris = "http://gateway_service:5000/api/v1/authorize/callback",
            AllowedScopes = "openid|profile|email",
            RequirePkce = false
        };

        db.Clients.Add(gateway);
        await db.SaveChangesAsync();
    }

    public static async Task EnsureSeededAsync(IClientStore clientStore, IdentityContext db)
    {
        if (await db.Clients.AnyAsync()) return;

        var gateway = new Client
        {
            ClientId = "gateway-client",
            ClientSecret = "secret",
            RedirectUris = "http://gateway_service:port/api/v1/authorize/callback",
            AllowedScopes = "openid|profile|email",
            RequirePkce = false
        };

        db.Clients.Add(gateway);
        await db.SaveChangesAsync();
    }
}

