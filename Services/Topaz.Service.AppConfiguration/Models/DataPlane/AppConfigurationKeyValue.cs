using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models.DataPlane;

public sealed class AppConfigurationKeyValue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string> Tags { get; set; } = [];

    [JsonPropertyName("etag")]
    public string Etag { get; set; } = string.Empty;

    [JsonPropertyName("last_modified")]
    public DateTimeOffset LastModified { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    public static AppConfigurationKeyValue Create(string key, string? label, string? value, string? contentType, Dictionary<string, string>? tags) =>
        new()
        {
            Key = key,
            Label = label,
            Value = value,
            ContentType = contentType,
            Tags = tags ?? [],
            Etag = Guid.NewGuid().ToString("N"),
            LastModified = DateTimeOffset.UtcNow,
            Locked = false
        };

    public void Update(string? value, string? contentType, Dictionary<string, string>? tags)
    {
        if (value != null) Value = value;
        if (contentType != null) ContentType = contentType;
        if (tags != null) Tags = tags;
        Etag = Guid.NewGuid().ToString("N");
        LastModified = DateTimeOffset.UtcNow;
    }

    /// <summary>Filesystem-safe ID derived from the key and label.</summary>
    public static string ToFileId(string key, string? label) =>
        Uri.EscapeDataString(key) + "__" + Uri.EscapeDataString(label ?? string.Empty);

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
