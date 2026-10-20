using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Extensions;

/// <summary>
/// 時間一律以伺服器本地時間 yyyy-MM-ddTHH:mm:ss 輸出（不含毫秒與時區）；於 AddJsonOptions 全域套用（DateTime? 自動適用）
/// </summary>
public class LocalDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            // 含時區的值轉為伺服器本地時間；不含時區者視為本地時間
            return parsed.Kind == DateTimeKind.Unspecified ? parsed : DateTime.SpecifyKind(parsed.ToLocalTime(), DateTimeKind.Unspecified);
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
