using Microsoft.EntityFrameworkCore;
using StatisticsService.Models;

namespace StatisticsService.Data;


public class StatisticsDbContext : DbContext
{
    public StatisticsDbContext(DbContextOptions<StatisticsDbContext> options) : base(options) { }

    public DbSet<UserActionEntity> UserActions => Set<UserActionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var ua = modelBuilder.Entity<UserActionEntity>();
        ua.ToTable("user_actions");

        ua.Property(p => p.UserId).HasMaxLength(128).IsRequired();
        ua.Property(p => p.Username).HasMaxLength(128).IsRequired();
        ua.Property(p => p.Action).HasMaxLength(128).IsRequired();
        ua.Property(p => p.Timestamp).IsRequired();

        ua.Property(p => p.MetadataJson).HasColumnType("jsonb").IsRequired(false);

        ua.Property(p => p.Topic).HasMaxLength(128).IsRequired();
        ua.Property(p => p.Partition).IsRequired();
        ua.Property(p => p.Offset).IsRequired();

        ua.HasIndex(p => new { p.Topic, p.Partition, p.Offset }).IsUnique();
    }
}

