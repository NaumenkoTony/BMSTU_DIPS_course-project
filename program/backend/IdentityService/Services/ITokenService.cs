using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityService.Services
{
    public interface ITokenService
    {
        Task<string> CreateAccessTokenAsync(string userId, string audience, IEnumerable<Claim> claims, string[] scopes);
        Task<string> CreateIdTokenAsync(string userId, string clientId, IEnumerable<Claim> claims, string[] scopes);
    }
}
