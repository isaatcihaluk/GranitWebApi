using System.Text.Json;
using System.Text.Json.Serialization;

namespace GranitWebApi.Converters;

public class LocalDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();

        if (value.Kind == DateTimeKind.Utc)
            return value.ToLocalTime();

        return value;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}