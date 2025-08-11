namespace IdentityService.Services;


using IdentityService.Models;
using System.Threading.Tasks;

public interface IClientStore
{
    Task<Client?> FindClientByIdAsync(string clientId);

    bool ValidateRedirectUri(Client client, string redirectUri);

    bool ValidateScopes(Client client, string[] scopes);

    bool ValidateClientSecret(Client client, string? secret);
}
