
namespace IdentityService.Models;

using System.ComponentModel.DataAnnotations;


public class Client
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string ClientId { get; set; } = null!;

    public string? ClientSecret { get; set; }

    public string RedirectUris { get; set; } = "";

    public string AllowedScopes { get; set; } = "openid|profile|email";

    public bool RequirePkce { get; set; } = true;

    public bool IsPublic { get; set; }
}