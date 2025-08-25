namespace IdentityService.Models;

using System.ComponentModel.DataAnnotations;

public class AuthorizationCode
{
    public required string Code { get; set; }
    public required string ClientId { get; set; }
    public required string UserId { get; set; }
    public required string RedirectUri { get; set; }
    public required string Scopes { get; set; }
    public required DateTime Expiration { get; set; }

    public string? CodeChallenge { get; set; }
    public string? CodeChallengeMethod { get; set; } 
}