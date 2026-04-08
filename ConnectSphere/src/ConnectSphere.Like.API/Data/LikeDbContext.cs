using ConnectSphere.Like.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Like.API.Data;

public class LikeDbContext : DbContext
{
    public LikeDbContext(DbContextOptions<LikeDbContext> options) : base(options) { }

    public DbSet<Models.Like> Likes => Set<Models.Like>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("like_svc");

        modelBuilder.Entity<Models.Like>(entity =>
        {
            entity.HasKey(l => l.LikeId);
            entity.Property(l => l.LikeId).UseIdentityAlwaysColumn();
            entity.Property(l => l.CreatedAt)
                  .HasColumnType("timestamp with time zone");

            entity.Property(l => l.TargetType)
                  .HasMaxLength(10);
        });
    }
}