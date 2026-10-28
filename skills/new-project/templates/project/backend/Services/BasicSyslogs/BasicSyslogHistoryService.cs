using System.Globalization;
using System.Text.Json;
using backend.Data;
using backend.DTOs.BasicSyslogs;
using backend.Models;
using backend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace backend.Services.BasicSyslogs;

public class BasicSyslogHistoryService(AppDbContext db, IOptions<BasicSyslogSettings> settings) : IBasicSyslogHistoryService
{
    private const int MaxItems = 200;
    private const string DeletedName = "（已刪除）";

    /// <summary>
    /// 外鍵以指向資料的名稱取代編號顯示（使用者顯示名稱、群組名稱、權限說明），不顯示編號與權限代碼；
    /// 業務資料表被其他資料表以外鍵參照時，於此加入其名稱欄位，未列出的資料表顯示原始值
    /// </summary>
    private static readonly Dictionary<string, Func<AppDbContext, List<int>, Task<Dictionary<int, string>>>> NameLoaders =
        new(StringComparer.Ordinal)
        {
            [nameof(BasicUser)] = (db, ids) => db.BasicUser.AsNoTracking()
                .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.DisplayName),
            [nameof(BasicGroup)] = (db, ids) => db.BasicGroup.AsNoTracking()
                .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name),
            [nameof(BasicPermission)] = (db, ids) => db.BasicPermission.AsNoTracking()
                .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Description ?? "（未說明）"),
        };

    public async Task<BasicSyslogHistoryResult> GetRecordHistoryAsync(string table, int id, params string[] relatedTables)
    {
        var retentionDays = settings.Value.RetentionDays;
        var cutoff = DateTime.Today.AddDays(-retentionDays);
        var idText = id.ToString(CultureInfo.InvariantCulture);
        var relatedJson = JsonSerializer.Serialize(relatedTables);
        var foreignKeyPath = $"$.Data.{table}Id";

        // 參數化查詢（OPENJSON／JSON_VALUE，SQL Server 2019 支援）；資料表名稱與路徑皆以參數傳入
        var logs = await db.BasicSyslog
            .FromSql($"""
                SELECT s.* FROM BasicSyslog AS s
                WHERE s.CreatedAt >= {cutoff}
                  AND EXISTS (
                    SELECT 1 FROM OPENJSON(s.BeforeData) AS j
                    WHERE (JSON_VALUE(j.value, '$.Table') = {table} AND JSON_VALUE(j.value, '$.Data.Id') = {idText})
                       OR (JSON_VALUE(j.value, '$.Table') IN (SELECT r.value FROM OPENJSON({relatedJson}) AS r)
                           AND JSON_VALUE(j.value, {foreignKeyPath}) = {idText})
                    UNION ALL
                    SELECT 1 FROM OPENJSON(s.AfterData) AS j
                    WHERE (JSON_VALUE(j.value, '$.Table') = {table} AND JSON_VALUE(j.value, '$.Data.Id') = {idText})
                       OR (JSON_VALUE(j.value, '$.Table') IN (SELECT r.value FROM OPENJSON({relatedJson}) AS r)
                           AND JSON_VALUE(j.value, {foreignKeyPath}) = {idText}))
                """)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(MaxItems)
            .ToListAsync();

        var related = relatedTables.ToHashSet(StringComparer.Ordinal);
        var foreignKey = $"{table}Id";
        var metas = GetMetas();
        var foreignFields = new List<(BasicSyslogHistoryField Field, string Table)>();

        var items = new List<BasicSyslogHistoryItem>();
        foreach (var log in logs)
        {
            var before = ParseRows(log.BeforeData, table, idText, related, foreignKey);
            var after = ParseRows(log.AfterData, table, idText, related, foreignKey);
            var rows = CompareRows(table, before, after, metas, foreignFields);
            if (rows.Count == 0)
            {
                continue;
            }

            items.Add(new BasicSyslogHistoryItem
            {
                Id = log.Id,
                Action = log.Action,
                Description = log.Description,
                Username = log.Username,
                CreatedAt = log.CreatedAt,
                Rows = rows,
            });
        }

        await ResolveForeignNamesAsync(foreignFields);
        return new BasicSyslogHistoryResult { RetentionDays = retentionDays, Items = items };
    }

    /// <summary>外鍵值換成名稱（每個資料表查詢一次）；指向的資料已刪除時顯示「（已刪除）」</summary>
    private async Task ResolveForeignNamesAsync(List<(BasicSyslogHistoryField Field, string Table)> foreignFields)
    {
        foreach (var group in foreignFields.GroupBy(f => f.Table, StringComparer.Ordinal))
        {
            var ids = group
                .SelectMany(f => new[] { ParseId(f.Field.Before), ParseId(f.Field.After) })
                .OfType<int>()
                .Distinct()
                .ToList();
            var names = ids.Count == 0 ? new Dictionary<int, string>() : await NameLoaders[group.Key](db, ids);

            foreach (var (field, _) in group)
            {
                field.Before = ToName(field.Before, names);
                field.After = ToName(field.After, names);
            }
        }
    }

    private static int? ParseId(string? raw)
        => int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;

    private static string? ToName(string? raw, Dictionary<int, string> names)
        => ParseId(raw) is { } id ? names.GetValueOrDefault(id, DeletedName) : raw;

    /// <summary>取出與該筆資料相關的資料列，以（資料表, 主鍵）為鍵</summary>
    private static Dictionary<(string Table, string Key), Dictionary<string, string?>> ParseRows(
        string json, string table, string idText, HashSet<string> related, string foreignKey)
    {
        var result = new Dictionary<(string, string), Dictionary<string, string?>>();
        using var document = JsonDocument.Parse(json);
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var rowTable = element.GetProperty("Table").GetString() ?? string.Empty;
            var data = element.GetProperty("Data");
            var values = data.EnumerateObject().ToDictionary(p => p.Name, p => ToText(p.Value), StringComparer.Ordinal);

            var isMain = rowTable == table && values.GetValueOrDefault("Id") == idText;
            var isRelated = related.Contains(rowTable) && values.GetValueOrDefault(foreignKey) == idText;
            if (!isMain && !isRelated)
            {
                continue;
            }

            // 主表以 Id 為鍵；關聯表（複合主鍵、只有鍵欄位）以全部欄位為鍵
            var key = values.TryGetValue("Id", out var rowId) && rowId is not null
                ? rowId
                : string.Join("|", values.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => $"{v.Key}={v.Value}"));
            result[(rowTable, key)] = values;
        }

        return result;
    }

    private static List<BasicSyslogHistoryRow> CompareRows(
        string mainTable,
        Dictionary<(string Table, string Key), Dictionary<string, string?>> before,
        Dictionary<(string Table, string Key), Dictionary<string, string?>> after,
        IReadOnlyDictionary<string, TableMeta> metas,
        List<(BasicSyslogHistoryField Field, string Table)> foreignFields)
    {
        var rows = new List<BasicSyslogHistoryRow>();
        foreach (var key in before.Keys.Union(after.Keys))
        {
            before.TryGetValue(key, out var beforeValues);
            after.TryGetValue(key, out var afterValues);
            var action = beforeValues is null ? "C" : afterValues is null ? "D" : "U";

            metas.TryGetValue(key.Table, out var meta);
            var names = (beforeValues?.Keys ?? Enumerable.Empty<string>()).Union(afterValues?.Keys ?? Enumerable.Empty<string>());
            var fields = new List<BasicSyslogHistoryField>();
            foreach (var name in names)
            {
                // 以原始值判斷是否變動（只差毫秒也算變動），顯示時再格式化
                var beforeValue = beforeValues?.GetValueOrDefault(name);
                var afterValue = afterValues?.GetValueOrDefault(name);
                if (action == "U" && beforeValue == afterValue && afterValue != "***（已變更）")
                {
                    continue;
                }

                // 主鍵（流水號）為內部編號，不顯示
                var field = meta?.Fields.GetValueOrDefault(name);
                if (field?.IsPrimaryKey == true)
                {
                    continue;
                }

                var historyField = new BasicSyslogHistoryField
                {
                    Name = name,
                    Label = field?.Label ?? name,
                    Before = Display(beforeValue, field?.ClrType),
                    After = Display(afterValue, field?.ClrType),
                };
                fields.Add(historyField);

                // 外鍵於全部歷程整理完後一次換成名稱
                if (field?.ForeignTable is { } foreignTable)
                {
                    foreignFields.Add((historyField, foreignTable));
                }
            }

            if (fields.Count == 0)
            {
                continue;
            }

            rows.Add(new BasicSyslogHistoryRow
            {
                Table = key.Table,
                TableLabel = meta?.Label ?? key.Table,
                Action = action,
                Fields = fields,
            });
        }

        // 主表在前，關聯表在後
        return rows.OrderBy(r => r.Table == mainTable ? 0 : 1).ThenBy(r => r.Table, StringComparer.Ordinal).ToList();
    }

    /// <param name="IsPrimaryKey">單一欄位主鍵（流水號），不顯示；關聯表的複合主鍵為外鍵，不在此列</param>
    /// <param name="ForeignTable">外鍵指向且列於 NameLoaders 的資料表，顯示時換成名稱</param>
    private sealed record FieldMeta(string Label, Type ClrType, bool IsPrimaryKey, string? ForeignTable);

    private sealed record TableMeta(string Label, Dictionary<string, FieldMeta> Fields);

    // 描述（Comment）只存在於設計時模型，執行時模型讀不到；模型在應用程式執行期間不變，讀取一次後快取
    private static IReadOnlyDictionary<string, TableMeta>? _metas;

    /// <summary>以資料表與欄位描述（HasComment）作為顯示名稱，並記錄欄位型別供格式化</summary>
    private IReadOnlyDictionary<string, TableMeta> GetMetas()
    {
        if (_metas is not null)
        {
            return _metas;
        }

        var metas = new Dictionary<string, TableMeta>(StringComparer.Ordinal);
        foreach (var entityType in db.GetService<IDesignTimeModel>().Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is null || metas.ContainsKey(tableName))
            {
                continue;
            }

            var keyProperties = entityType.FindPrimaryKey()?.Properties;
            var singleKey = keyProperties is { Count: 1 } ? keyProperties[0] : null;
            var fields = entityType.GetProperties().ToDictionary(p => p.Name, p =>
            {
                var label = p.GetComment() ?? p.Name;
                var foreignTable = p.GetContainingForeignKeys()
                    .Select(fk => fk.PrincipalEntityType.GetTableName())
                    .FirstOrDefault(t => t is not null && NameLoaders.ContainsKey(t));
                return new FieldMeta(
                    foreignTable is null ? label : ToForeignLabel(label),
                    p.ClrType,
                    ReferenceEquals(p, singleKey),
                    foreignTable);
            }, StringComparer.Ordinal);
            metas[tableName] = new TableMeta(entityType.GetComment() ?? tableName, fields);
        }

        _metas = metas;
        return metas;
    }

    /// <summary>
    /// 外鍵顯示名稱：去除描述中指向的資料表欄位與結尾的「編號」，如「權限編號（BasicPermission.Id）」→「權限」
    /// </summary>
    private static string ToForeignLabel(string comment)
    {
        var index = comment.IndexOf('（');
        var label = (index > 0 ? comment[..index] : comment).Trim();
        return label.Length > 2 && label.EndsWith("編號", StringComparison.Ordinal) ? label[..^2] : label;
    }

    /// <summary>JSON 值轉為原始文字（先以原始值判斷是否變動，顯示時再格式化）</summary>
    private static string? ToText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => value.GetString(),
        _ => value.GetRawText(),
    };

    /// <summary>依欄位型別格式化顯示值：時間去除毫秒、布林顯示是／否</summary>
    private static string? Display(string? raw, Type? clrType)
    {
        if (raw is null || clrType is null)
        {
            return raw;
        }

        var type = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (type == typeof(DateTime)
            && DateTime.TryParseExact(raw, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return raw[..19];
        }

        if (type == typeof(bool))
        {
            return raw == "true" ? "是" : raw == "false" ? "否" : raw;
        }

        return raw;
    }
}
