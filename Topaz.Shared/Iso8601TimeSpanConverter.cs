using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Topaz.Shared;

/// <summary>
/// Converts TimeSpan values to and from ISO 8601 duration format (e.g., "PT1M", "P10675199DT2H48M5.4775807S").
/// This is required for Azure SDK compatibility which uses ISO 8601 standard for duration values.
/// </summary>
public class Iso8601TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return TimeSpan.Zero;
        }

        try
        {
            return XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException)
        {
            // Fallback to standard TimeSpan parsing if not ISO 8601
            return TimeSpan.Parse(value);
        }
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(XmlConvert.ToString(value));
    }
}

/// <summary>
/// Converts nullable TimeSpan values to and from ISO 8601 duration format.
/// </summary>
public class Iso8601NullableTimeSpanConverter : JsonConverter<TimeSpan?>
{
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException)
        {
            // Fallback to standard TimeSpan parsing if not ISO 8601
            return TimeSpan.Parse(value);
        }
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(XmlConvert.ToString(value.Value));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
