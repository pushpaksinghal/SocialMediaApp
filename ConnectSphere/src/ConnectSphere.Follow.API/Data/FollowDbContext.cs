using ConnectSphere.Follow.API.Models;
using Microsoft.EntityFrameworkCore;
namespace ConnectSphere.Follow.API.Data;

public class FollowDbContext : DbContext
{
    public FollowDbContext(DbContextOptions<FollowDbContext> options)
        : base(options) { }

    public DbSet<Models.Follows> Follows => Set<Models.Follows>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("follow");

        modelBuilder.Entity<Models.Follows>(entity =>
        {
            entity.HasKey(f => f.FollowId);
            entity.Property(f => f.FollowId).UseIdentityAlwaysColumn();
            entity.Property(f => f.CreatedAt)
                  .HasColumnType("timestamp with time zone");
            entity.Property(f => f.Status).HasMaxLength(12);
        });
    }
}