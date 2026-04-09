using ConnectSphere.Feed.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Feed.API.Data;

public class FeedDbContext : DbContext
{
    public FeedDbContext(DbContextOptions<FeedDbContext> options)
        : base(options) { }

    public DbSet<FeedItem> FeedItems => Set<FeedItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("feed");

        modelBuilder.Entity<FeedItem>(entity =>
        {
            entity.HasKey(f => f.FeedItemId);
            entity.Property(f => f.FeedItemId).UseIdentityAlwaysColumn();
            entity.Property(f => f.CreatedAt)
                  .HasColumnType("timestamp with time zone");
            entity.Property(f => f.ExpiresAt)
                  .HasColumnType("timestamp with time zone");
            entity.Property(f => f.Score)
                  .HasColumnType("decimal(18,4)");
        });
    }
}