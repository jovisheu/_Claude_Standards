using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

/// <summary>
/// 資料表與欄位一律以 HasComment 加上繁體中文描述（寫入 SQL Server 的 MS_Description）；
/// 描述只說明欄位本身的意義，不寫特定資料的值或程式約定。
/// 一律由 DI 取得（掛有異動歷程攔截器），不可自行 new。
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<BasicUser> BasicUser => Set<BasicUser>();
    public DbSet<BasicGroup> BasicGroup => Set<BasicGroup>();
    public DbSet<BasicPermission> BasicPermission => Set<BasicPermission>();
    public DbSet<BasicUserGroup> BasicUserGroup => Set<BasicUserGroup>();
    public DbSet<BasicGroupPermission> BasicGroupPermission => Set<BasicGroupPermission>();
    public DbSet<BasicLoginErrlog> BasicLoginErrlog => Set<BasicLoginErrlog>();
    public DbSet<BasicPwdlog> BasicPwdlog => Set<BasicPwdlog>();
    public DbSet<BasicChecklogintimeSendMailLog> BasicChecklogintimeSendMailLog => Set<BasicChecklogintimeSendMailLog>();
    public DbSet<BasicExceptionlog> BasicExceptionlog => Set<BasicExceptionlog>();
    public DbSet<BasicSyslog> BasicSyslog => Set<BasicSyslog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BasicUser>(e =>
        {
            e.ToTable("BasicUser", t => t.HasComment("使用者"));
            e.Property(x => x.Id).HasComment("流水號");
            e.Property(x => x.Username).HasMaxLength(50).IsRequired().HasComment("登入帳號");
            e.Property(x => x.Email).HasMaxLength(256).IsRequired().HasComment("電子郵件（寄送重設密碼連結）");
            e.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired().HasComment("密碼雜湊（BCrypt）");
            e.Property(x => x.DisplayName).HasMaxLength(50).IsRequired().HasComment("顯示名稱");
            e.Property(x => x.IsActive).HasComment("是否啟用（停用者不可登入）");
            e.Property(x => x.CreatedAt).HasComment("建立時間");
            e.Property(x => x.UpdatedAt).HasComment("最後修改時間");
            e.Property(x => x.PasswordResetTokenHash).HasMaxLength(64).HasComment("重設密碼 token 的 SHA-256 雜湊（不存 token 本身）");
            e.Property(x => x.PasswordResetExpiresAt).HasComment("重設密碼連結到期時間");
            e.HasIndex(x => x.Username).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
            // 可為 null 的欄位不建唯一索引，改建一般索引
            e.HasIndex(x => x.PasswordResetTokenHash);
            e.HasData(SeedData.BasicUser);
        });

        modelBuilder.Entity<BasicGroup>(e =>
        {
            e.ToTable("BasicGroup", t => t.HasComment("群組（角色）"));
            e.Property(x => x.Id).HasComment("流水號");
            e.Property(x => x.Name).HasMaxLength(50).IsRequired().HasComment("群組名稱");
            e.Property(x => x.Description).HasMaxLength(200).HasComment("群組說明");
            e.Property(x => x.CreatedAt).HasComment("建立時間");
            e.Property(x => x.UpdatedAt).HasComment("最後修改時間");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(SeedData.BasicGroup);
        });

        modelBuilder.Entity<BasicPermission>(e =>
        {
            e.ToTable("BasicPermission", t => t.HasComment("權限（以靜態種子建立）"));
            e.Property(x => x.Id).ValueGeneratedNever().HasComment("權限編號（由種子指定）");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired().HasComment("權限名稱（{資料表小寫}.{動作}，與授權 Policy 同名）");
            e.Property(x => x.Description).HasMaxLength(200).HasComment("權限說明");
            e.Property(x => x.Module).HasMaxLength(50).IsRequired().HasComment("所屬功能模組（畫面分組用）");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(SeedData.BasicPermission);
        });

        modelBuilder.Entity<BasicUserGroup>(e =>
        {
            e.ToTable("BasicUserGroup", t => t.HasComment("使用者與群組對應"));
            e.HasKey(x => new { x.BasicUserId, x.BasicGroupId });
            e.Property(x => x.BasicUserId).HasComment("使用者編號（BasicUser.Id）");
            e.Property(x => x.BasicGroupId).HasComment("群組編號（BasicGroup.Id）");
            e.HasOne(x => x.BasicUser).WithMany(x => x.BasicUserGroup).HasForeignKey(x => x.BasicUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.BasicGroup).WithMany(x => x.BasicUserGroup).HasForeignKey(x => x.BasicGroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasData(SeedData.BasicUserGroup);
        });

        modelBuilder.Entity<BasicGroupPermission>(e =>
        {
            e.ToTable("BasicGroupPermission", t => t.HasComment("群組與權限對應"));
            e.HasKey(x => new { x.BasicGroupId, x.BasicPermissionId });
            e.Property(x => x.BasicGroupId).HasComment("群組編號（BasicGroup.Id）");
            e.Property(x => x.BasicPermissionId).HasComment("權限編號（BasicPermission.Id）");
            e.HasOne(x => x.BasicGroup).WithMany(x => x.BasicGroupPermission).HasForeignKey(x => x.BasicGroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.BasicPermission).WithMany(x => x.BasicGroupPermission).HasForeignKey(x => x.BasicPermissionId).OnDelete(DeleteBehavior.Cascade);
            e.HasData(SeedData.BasicGroupPermission);
        });

        modelBuilder.Entity<BasicLoginErrlog>(e =>
        {
            e.ToTable("BasicLoginErrlog", t => t.HasComment("登入失敗紀錄（用於鎖定判斷）"));
            e.Property(x => x.Id).HasComment("流水號");
            e.Property(x => x.Username).HasMaxLength(50).IsRequired().HasComment("登入時輸入的帳號（帳號不存在也記錄）");
            e.Property(x => x.CreatedAt).HasComment("登入失敗時間");
            e.HasIndex(x => new { x.Username, x.CreatedAt });
        });

        modelBuilder.Entity<BasicPwdlog>(e =>
        {
            e.ToTable("BasicPwdlog", t => t.HasComment("密碼歷程（用於重複與到期判斷）"));
            e.Property(x => x.Id).HasComment("流水號");
            e.Property(x => x.Username).HasMaxLength(50).IsRequired().HasComment("帳號");
            e.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired().HasComment("當時設定的密碼雜湊（判斷不得與前 N 次相同）");
            e.Property(x => x.CreatedAt).HasComment("設定時間（判斷密碼是否到期）");
            e.HasIndex(x => new { x.Username, x.CreatedAt });
            e.HasData(SeedData.BasicPwdlog);
        });

        modelBuilder.Entity<BasicChecklogintimeSendMailLog>(e =>
        {
            e.ToTable("BasicChecklogintimeSendMailLog", t => t.HasComment("密碼即將到期通知寄送紀錄（用於判斷當日是否已寄送）"));
            e.Property(x => x.Id).HasComment("流水號");
            e.Property(x => x.Username).HasMaxLength(50).IsRequired().HasComment("帳號");
            e.Property(x => x.Email).HasMaxLength(256).IsRequired().HasComment("寄送時的電子郵件");
            e.Property(x => x.CreatedAt).HasComment("寄送時間");
            e.HasIndex(x => new { x.Username, x.CreatedAt });
        });

        modelBuilder.Entity<BasicExceptionlog>(e =>
        {
            e.ToTable("BasicExceptionlog", t => t.HasComment("系統錯誤紀錄（超過保留天數轉存 txt 後刪除）"));
            e.Property(x => x.Id).HasComment("流水號");
            e.Property(x => x.ExceptionType).HasMaxLength(200).IsRequired().HasComment("例外類別（含 namespace）");
            e.Property(x => x.MethodName).HasMaxLength(200).IsRequired().HasComment("發生位置（如 BasicUsersController.Create），也是轉存檔名");
            e.Property(x => x.Message).IsRequired().HasComment("錯誤訊息");
            e.Property(x => x.StackTrace).HasComment("呼叫堆疊");
            e.Property(x => x.Username).HasMaxLength(50).HasComment("登入者帳號（未登入或背景作業為 null）");
            e.Property(x => x.CreatedAt).HasComment("發生時間");
            e.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<BasicSyslog>(e =>
        {
            e.ToTable("BasicSyslog", t => t.HasComment("系統異動歷程（超過保留天數轉存 txt 後刪除）"));
            e.Property(x => x.Id).HasComment("流水號");
            e.Property(x => x.Action).HasMaxLength(1).IsFixedLength().IsRequired().HasComment("類別（C 新增、U 修改、D 刪除）");
            e.Property(x => x.MethodName).HasMaxLength(200).IsRequired().HasComment("異動來源（如 BasicUsersController.Update），也是轉存檔名");
            e.Property(x => x.Description).HasMaxLength(BasicSyslogContext.DescriptionMaxLength).HasComment("業務說明（選填，如審核通過、批次匯入）");
            e.Property(x => x.BeforeData).IsRequired().HasComment("異動前資料（JSON 陣列，新增時為 []，機密欄位以 *** 遮蔽）");
            e.Property(x => x.AfterData).IsRequired().HasComment("異動後資料（JSON 陣列，刪除時為 []，機密欄位以 *** 遮蔽）");
            e.Property(x => x.Username).HasMaxLength(50).HasComment("登入者帳號（未登入為 null）");
            e.Property(x => x.CreatedAt).HasComment("異動時間");
            e.HasIndex(x => x.CreatedAt);
        });
    }
}
