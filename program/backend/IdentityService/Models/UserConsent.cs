namespace IdentityService.Models
{
    public class UserConsent
    {
        public Guid UserId { get; set; }
        public string ClientId { get; set; } = null!;
        public string Scopes { get; set; } = null!;
    }
}