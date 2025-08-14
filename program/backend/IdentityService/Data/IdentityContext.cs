using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IdentityService.Models;

namespace IdentityService.Data
{
    public class IdentityContext : IdentityDbContext<User, Role, Guid>
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<AuthorizationCode> AuthorizationCodes { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<UserConsent> UserConsents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Client>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.ClientId).IsRequired();
                entity.Property(c => c.ClientSecret).IsRequired();
                entity.Property(c => c.RedirectUris).IsRequired();
                entity.Property(c => c.AllowedScopes).IsRequired();
            });

            builder.Entity<AuthorizationCode>(entity =>
            {
                entity.HasKey(c => c.Code);
                entity.Property(c => c.ClientId).IsRequired();
                entity.Property(c => c.UserId).IsRequired();
                entity.Property(c => c.RedirectUri).IsRequired();
                entity.Property(c => c.Scopes).IsRequired();
                entity.Property(c => c.Expiration).IsRequired();
            });

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(r => r.Token);
                entity.Property(r => r.UserId).IsRequired();
                entity.Property(r => r.ClientId).IsRequired();
                entity.Property(r => r.Expiration).IsRequired();
            });

            builder.Entity<UserConsent>(entity =>
            {
                entity.HasKey(c => new { c.UserId, c.ClientId });
                entity.Property(c => c.Scopes).IsRequired();
            });
        }
    }
}