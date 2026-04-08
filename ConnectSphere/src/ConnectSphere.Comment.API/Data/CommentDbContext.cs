using ConnectSphere.Comment.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Comment.API.Data;

public class CommentDbContext : DbContext
{
    public CommentDbContext(DbContextOptions<CommentDbContext> options)
        : base(options) { }

    public DbSet<Models.Comment> Comments => Set<Models.Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("comment");

        modelBuilder.Entity<Models.Comment>(entity =>
        {
            entity.HasKey(c => c.CommentId);
            entity.Property(c => c.CommentId).UseIdentityAlwaysColumn();

            entity.Property(c => c.CreatedAt)
                  .HasColumnType("timestamp with time zone");
            entity.Property(c => c.EditedAt)
                  .HasColumnType("timestamp with time zone");

            // Self-referential FK for replies
            entity.HasOne<Models.Comment>()
                  .WithMany()
                  .HasForeignKey(c => c.ParentCommentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}