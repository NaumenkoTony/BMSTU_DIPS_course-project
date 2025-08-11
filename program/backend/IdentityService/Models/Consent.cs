
namespace IdentityService.Models;

using System.ComponentModel.DataAnnotations;

public class Consent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string ClientId { get; set; } = null!;

    public string Scopes { get; set; } = "";
}