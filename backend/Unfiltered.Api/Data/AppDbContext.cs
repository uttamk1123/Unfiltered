using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Unfiltered.Api.Models;

namespace Unfiltered.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Flag> Flags => Set<Flag>();
    public DbSet<ModerationLogEntry> ModerationLog => Set<ModerationLogEntry>();

    // All timestamps are stored in UTC; mark them as UTC when read so JSON carries the "Z" suffix.
    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.PasswordHash).HasMaxLength(256);
            e.Property(u => u.DisplayName).HasMaxLength(40);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(u => u.Email).IsUnique();
        });

        b.Entity<Listing>(e =>
        {
            e.Property(l => l.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(l => l.Name).HasMaxLength(150);
            e.Property(l => l.Platform).HasMaxLength(100);
            e.Property(l => l.Url).HasMaxLength(500);
            e.HasIndex(l => l.Url).IsUnique().HasFilter("[Url] IS NOT NULL");
            e.HasIndex(l => l.Name);
        });

        b.Entity<Review>(e =>
        {
            e.Property(r => r.Title).HasMaxLength(120);
            e.Property(r => r.Tags).HasMaxLength(200);
            e.Property(r => r.BoughtOn).HasMaxLength(60);
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.ProofFile).HasMaxLength(64);
            e.Property(r => r.RejectionFile).HasMaxLength(64);
            e.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] BETWEEN 1 AND 5"));
            e.HasOne(r => r.Listing).WithMany(l => l.Reviews).HasForeignKey(r => r.ListingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User).WithMany(u => u.Reviews).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.ListingId, r.Status });
            e.HasIndex(r => new { r.Status, r.CreatedAt });
        });

        b.Entity<Flag>(e =>
        {
            e.Property(f => f.Reason).HasMaxLength(1000);
            e.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(f => f.Review).WithMany(r => r.Flags).HasForeignKey(f => f.ReviewId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(f => f.Status);
        });

        b.Entity<ModerationLogEntry>(e =>
        {
            e.ToTable("ModerationLog");
            e.Property(m => m.Action).HasMaxLength(100);
            e.Property(m => m.Reason).HasMaxLength(1000);
            e.HasOne(m => m.Review).WithMany(r => r.Log).HasForeignKey(m => m.ReviewId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Moderator).WithMany().HasForeignKey(m => m.ModeratorId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
