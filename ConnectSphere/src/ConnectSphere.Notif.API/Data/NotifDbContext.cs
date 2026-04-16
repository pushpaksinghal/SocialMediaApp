using ConnectSphere.Notif.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Notif.API.Data;

public class NotifDbContext : DbContext
{
    public NotifDbContext(DbContextOptions<NotifDbContext> options)
        : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("notification");

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.NotificationId);
            entity.Property(n => n.NotificationId).UseIdentityAlwaysColumn();
            entity.Property(n => n.CreatedAt)
                  .HasColumnType("timestamp with time zone");
            entity.Property(n => n.Type).HasMaxLength(20);
            entity.Property(n => n.Message).HasMaxLength(512);
            entity.Property(n => n.TargetType).HasMaxLength(10);
        });
    }
}