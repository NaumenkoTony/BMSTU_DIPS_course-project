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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Client>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.ClientId).IsRequired();
                entity.Property(c => c.ClientSecret);
                entity.Property(c => c.RedirectUris).IsRequired();
                entity.Property(c => c.AllowedScopes).IsRequired();
                entity.Property(c => c.RequirePkce);
                
                entity.HasIndex(c => c.ClientId).IsUnique();
            });

            builder.Entity<AuthorizationCode>(entity =>
            {
                entity.HasKey(c => c.Code);
                entity.Property(c => c.Code).IsRequired();
                entity.Property(c => c.ClientId).IsRequired();
                entity.Property(c => c.UserId).IsRequired();
                entity.Property(c => c.RedirectUri).IsRequired();
                entity.Property(c => c.Scopes).IsRequired();
                entity.Property(c => c.Expiration).IsRequired();
                entity.Property(c => c.CodeChallenge).IsRequired(false);;
                entity.Property(c => c.CodeChallengeMethod).IsRequired(false);;
                
                entity.HasIndex(c => c.Expiration);
            });
        }
    }
}