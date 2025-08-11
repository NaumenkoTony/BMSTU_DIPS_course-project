namespace IdentityService.Models;

using System.ComponentModel.DataAnnotations;

public class AuthorizationCode
{
    public string Code { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string RedirectUri { get; set; } = null!;
    public string Scopes { get; set; } = null!;
    public DateTime Expiration { get; set; }
}