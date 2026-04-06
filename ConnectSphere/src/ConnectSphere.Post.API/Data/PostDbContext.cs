using ConnectSphere.Post.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Post.API.Data;

public class PostDbContext : DbContext
{
    public PostDbContext(DbContextOptions<PostDbContext> options) : base(options) { }

    public DbSet<Models.Post> Posts => Set<Models.Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("post");

        modelBuilder.Entity<Models.Post>(entity =>
        {
            entity.HasKey(p => p.PostId);
            entity.Property(p => p.PostId).UseIdentityAlwaysColumn();
            entity.Property(p => p.CreatedAt).HasColumnType("timestamp with time zone");
            entity.Property(p => p.UpdatedAt).HasColumnType("timestamp with time zone");

            entity.HasOne<Models.Post>()
                  .WithMany()
                  .HasForeignKey(p => p.OriginalPostId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}