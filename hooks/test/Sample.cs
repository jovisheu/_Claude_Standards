// 規範檢查的測試樣本：每一行都應被 check-standards.js 標示（不參與任何建置）
public class Sample
{
    public void Run()
    {
        var now = DateTime.UtcNow;
        db.SaveChanges();
        db.Bulletin.Where(b => b.Id == 1).ExecuteDelete();
        var r = new Random();
        db.Bulletin.FromSqlRaw($"SELECT * FROM Bulletin WHERE Title = '{title}'");
        logger.LogInformation($"新增公告：{id}");
        db.Database.ExecuteSqlRaw("UPDATE Bulletin SET Title = {0}", title);
        using var other = new AppDbContext(options);
        using (tracker.AllowLogArchiveDelete("BasicSyslog")) { }
    }
}
