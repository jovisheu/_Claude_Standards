using System.Globalization;
using System.Text.Json;
using backend.Data;
using backend.DTOs.BasicSyslogs;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace backend.Services.BasicSyslogs;

public class BasicSyslogHistoryService(AppDbContext db, IOptions<BasicSyslogSettings> settingsOptions) : IBasicSyslogHistoryService
{
    /// <summary>單筆資料最多回傳的歷程筆數</summary>
    private const int MaxItems = 200;

    public async Task<BasicSyslogHistoryResult> GetRecordHistoryAsync(string tableName, int recordId, params string[] relatedTableNames)
    {
        var idText = recordId.ToString(CultureInfo.InvariantCulture);

        // 以 OPENJSON 比對異動前後資料中的主表 Id（參數化查詢；SQL Server 2019 支援）
        var basicSyslogs = await db.BasicSyslog
            .FromSql($"""
                SELECT s.* FROM BasicSyslog s
                WHERE EXISTS (SELECT 1 FROM OPENJSON(s.AfterData) j
                              WHERE JSON_VALUE(j.[value], '$.Table') = {tableName} AND JSON_VALUE(j.[value], '$.Data.Id') = {idText})
                   OR EXISTS (SELECT 1 FROM OPENJSON(s.BeforeData) j
                              WHERE JSON_VALUE(j.[value], '$.Table') = {tableName} AND JSON_VALUE(j.[value], '$.Data.Id') = {idText})
                """)
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Take(MaxItems)
            .ToListAsync();

        var foreignKey = tableName + "Id";
        bool IsRelevant(string table, JsonElement data) =>
            (table == tableName && GetText(data, "Id") == idText)
            || (relatedTableNames.Contains(table) && GetText(data, foreignKey) == idText);

        return new BasicSyslogHistoryResult
        {
            RetentionDays = settingsOptions.Value.RetentionDays,
            Items = basicSyslogs.Select(s => new BasicSyslogHistoryItem
            {
                Id = s.Id,
                Action = s.Action,
                Description = s.Description,
                Username = s.Username,
                CreatedAt = s.CreatedAt,
                Rows = BuildRows(s.BeforeData, s.AfterData, IsRelevant)
            }).ToList()
        };
    }

    /// <summary>
    /// 將異動前後的資料依資料表與鍵值配對：只在異動前為刪除、只在異動後為新增、兩者皆有為修改（只列出有變動的欄位）
    /// </summary>
    private List<BasicSyslogHistoryRow> BuildRows(string beforeJson, string afterJson, Func<string, JsonElement, bool> isRelevant)
    {
        using var beforeDoc = JsonDocument.Parse(beforeJson);
        using var afterDoc = JsonDocument.Parse(afterJson);

        var before = ReadRows(beforeDoc, isRelevant);
        var after = ReadRows(afterDoc, isRelevant);

        var rows = new List<BasicSyslogHistoryRow>();
        foreach (var key in after.Keys.Concat(before.Keys).Distinct())
        {
            before.TryGetValue(key, out var b);
            after.TryGetValue(key, out var a);
            var table = (a ?? b)!.Table;
            var action = b == null ? "C" : a == null ? "D" : "U";

            var fields = (a?.Values.Keys.AsEnumerable() ?? []).Concat(b?.Values.Keys.AsEnumerable() ?? []).Distinct()
                .Select(name => (Name: name, Before: b?.Values.GetValueOrDefault(name), After: a?.Values.GetValueOrDefault(name)))
                // 以儲存的原始值判斷是否變動，再轉成顯示格式
                .Where(f => action != "U" || f.Before != f.After)
                .Select(f => new BasicSyslogHistoryField
                {
                    Name = f.Name,
                    Description = GetColumnDescription(table, f.Name),
                    Before = FormatValue(table, f.Name, f.Before),
                    After = FormatValue(table, f.Name, f.After)
                })
                .ToList();
            if (fields.Count == 0) continue;

            rows.Add(new BasicSyslogHistoryRow
            {
                Table = table,
                TableDescription = GetTableDescription(table),
                Action = action,
                Fields = fields
            });
        }
        return rows;
    }

    private static Dictionary<string, SnapshotRow> ReadRows(JsonDocument doc, Func<string, JsonElement, bool> isRelevant)
    {
        var rows = new Dictionary<string, SnapshotRow>();
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var table = element.GetProperty("Table").GetString() ?? string.Empty;
            var data = element.GetProperty("Data");
            if (!isRelevant(table, data)) continue;

            var values = data.EnumerateObject().ToDictionary(p => p.Name, p => ToText(p.Value));
            // 單一主鍵以 Id 配對；關聯表（複合主鍵）以所有 *Id 欄位配對
            var key = values.TryGetValue("Id", out var id)
                ? $"{table}|{id}"
                : $"{table}|{string.Join(",", values.Where(v => v.Key.EndsWith("Id", StringComparison.Ordinal)).OrderBy(v => v.Key).Select(v => $"{v.Key}={v.Value}"))}";
            rows[key] = new SnapshotRow(table, values);
        }
        return rows;
    }

    private static string? GetText(JsonElement data, string name) =>
        data.TryGetProperty(name, out var value) ? ToText(value) : null;

    private static string? ToText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => "是",
        JsonValueKind.False => "否",
        _ => value.GetRawText()
    };

    private string GetTableDescription(string table) =>
        GetDescriptions().TryGetValue(table, out var t) ? t.Description : table;

    private string GetColumnDescription(string table, string column) =>
        GetDescriptions().TryGetValue(table, out var t) && t.Columns.TryGetValue(column, out var description) ? description : column;

    /// <summary>
    /// 時間欄位去除毫秒（歷程儲存為 yyyy-MM-dd HH:mm:ss.fff，回傳時與其他 API 一致不含毫秒）
    /// </summary>
    private string? FormatValue(string table, string column, string? value)
    {
        if (value == null
            || !GetDescriptions().TryGetValue(table, out var t)
            || !t.DateTimeColumns.Contains(column)
            || !DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return value;
        }
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 資料表與欄位描述（HasComment）只存在 design-time model，第一次使用時讀取後快取
    /// </summary>
    private IReadOnlyDictionary<string, TableDescription> GetDescriptions() =>
        _descriptions ??= db.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Where(e => e.GetTableName() != null)
            .GroupBy(e => e.GetTableName()!)
            .ToDictionary(g => g.Key, g =>
            {
                var entityType = g.First();
                var properties = entityType.GetProperties().ToList();
                return new TableDescription(
                    entityType.GetComment() ?? g.Key,
                    properties.ToDictionary(p => p.Name, p => p.GetComment() ?? p.Name),
                    properties.Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(DateTime)).Select(p => p.Name).ToHashSet());
            });

    private static IReadOnlyDictionary<string, TableDescription>? _descriptions;

    private sealed record TableDescription(string Description, Dictionary<string, string> Columns, HashSet<string> DateTimeColumns);

    private sealed record SnapshotRow(string Table, Dictionary<string, string?> Values);
}
