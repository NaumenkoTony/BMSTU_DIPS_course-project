
namespace IdentityService.Services;

using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;


public class ClientStore : IClientStore
{
    private readonly IdentityContext _context;

    public ClientStore(IdentityContext context)
    {
        _context = context;
    }

    public async Task<Client?> FindClientByIdAsync(string clientId)
    {
        return await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public bool ValidateRedirectUri(Client client, string redirectUri)
    {
        if (string.IsNullOrEmpty(redirectUri))
            return false;

        var uris = client.RedirectUris.Split('|', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        return uris.Contains(redirectUri);
    }

    public bool ValidateScopes(Client client, string[] scopes)
    {
        var allowedScopes = client.AllowedScopes.Split('|', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        return scopes.All(s => allowedScopes.Contains(s));
    }

    public bool ValidateClientSecret(Client client, string? secret)
    {
        if (client.IsPublic)
            return true;

        return client.ClientSecret == secret;
    }
}

