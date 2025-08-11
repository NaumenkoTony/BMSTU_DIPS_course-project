namespace IdentityService.Services;

using System.Threading.Tasks;
using IdentityService.Models;

public interface IAuthorizationCodeStore
{
    Task SaveCodeAsync(AuthorizationCode code);
    Task<AuthorizationCode?> FindCodeAsync(string code);
    Task RemoveCodeAsync(string code);
}