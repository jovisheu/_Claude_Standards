using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();
    public DbSet<LoginErrLog> LoginErrLogs => Set<LoginErrLog>();
    public DbSet<PwdLog> PwdLogs => Set<PwdLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordResetTokenHash).HasMaxLength(64);
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            // 可為 null 的欄位不建唯一索引，改建一般索引
            e.HasIndex(x => x.PasswordResetTokenHash);
            e.HasData(SeedData.Users);
        });

        modelBuilder.Entity<Group>(e =>
        {
            e.ToTable("Groups");
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(SeedData.Groups);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.Module).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(SeedData.Permissions);
        });

        modelBuilder.Entity<UserGroup>(e =>
        {
            e.ToTable("UserGroups");
            e.HasKey(x => new { x.UserId, x.GroupId });
            e.HasOne(x => x.User).WithMany(x => x.UserGroups).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Group).WithMany(x => x.UserGroups).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasData(SeedData.UserGroups);
        });

        modelBuilder.Entity<GroupPermission>(e =>
        {
            e.ToTable("GroupPermissions");
            e.HasKey(x => new { x.GroupId, x.PermissionId });
            e.HasOne(x => x.Group).WithMany(x => x.GroupPermissions).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Permission).WithMany(x => x.GroupPermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
            e.HasData(SeedData.GroupPermissions);
        });

        modelBuilder.Entity<LoginErrLog>(e =>
        {
            e.ToTable("_LoginErr_log");
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.Username, x.CreatedAt });
        });

        modelBuilder.Entity<PwdLog>(e =>
        {
            e.ToTable("_Pwd_log");
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.Username, x.CreatedAt });
            e.HasData(SeedData.PwdLogs);
        });
    }
}
