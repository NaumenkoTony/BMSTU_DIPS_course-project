using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityService.Services
{
    public interface ITokenService
    {
        Task<string> CreateIdTokenAsync(string userId, string clientId, IEnumerable<Claim> userClaims, IEnumerable<string> scopes);
        Task<string> CreateAccessTokenAsync(string userId, string clientId, IEnumerable<Claim> userClaims, IEnumerable<string> scopes);
    }
}
