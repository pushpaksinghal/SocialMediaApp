using ConnectSphere.Auth.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ConnectSphere.Auth.API.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("auth");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.UserId).UseIdentityAlwaysColumn();
            entity.Property(u => u.CreatedAt).HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.AuditLogId);
            entity.Property(a => a.AuditLogId).UseIdentityAlwaysColumn();
            entity.Property(a => a.Timestamp).HasColumnType("timestamp with time zone");
        });
    }
}