namespace IdentityService.Models
{
    public class RefreshToken
    {
        public string Token { get; set; } = null!;
        public Guid UserId { get; set; }
        public string ClientId { get; set; } = null!;
        public DateTime Expiration { get; set; }
    }
}