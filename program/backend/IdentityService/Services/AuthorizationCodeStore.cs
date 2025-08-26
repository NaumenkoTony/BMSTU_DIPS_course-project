
namespace IdentityService.Services;

using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


public class AuthorizationCodeStore : IAuthorizationCodeStore
{
    private readonly IdentityContext _context;
    private readonly ILogger<AuthorizationCodeStore> _logger;

    public AuthorizationCodeStore(IdentityContext context, ILogger<AuthorizationCodeStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveCodeAsync(AuthorizationCode code)
    {
        _logger.LogInformation("Saving code: {Code}, Challenge: {Challenge}, Method: {Method}", 
        code.Code, code.CodeChallenge, code.CodeChallengeMethod);

        _context.AuthorizationCodes.Add(code);
        await _context.SaveChangesAsync();
    }

    public async Task<AuthorizationCode?> FindCodeAsync(string code)
    {
        return await _context.AuthorizationCodes.FirstOrDefaultAsync(c => c.Code == code);
    }

    public async Task RemoveCodeAsync(string code)
    {
        var entity = await _context.AuthorizationCodes.FirstOrDefaultAsync(c => c.Code == code);
        if (entity != null)
        {
            _context.AuthorizationCodes.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}

