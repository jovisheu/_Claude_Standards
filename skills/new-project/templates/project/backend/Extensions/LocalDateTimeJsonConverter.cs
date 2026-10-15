using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Extensions;

/// <summary>
/// API 回傳的時間一律為本地時間 yyyy-MM-ddTHH:mm:ss（不含毫秒與時區），前端直接以本地時間解析；
/// DateTime? 由 System.Text.Json 自動套用此轉換器
/// </summary>
public sealed class LocalDateTimeJsonConverter : JsonConverter<DateTime>
{
    public const string Format = "yyyy-MM-ddTHH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
