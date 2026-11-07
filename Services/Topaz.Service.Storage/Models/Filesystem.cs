using System.Text;
using System.Text.Json.Serialization;
using Azure;
using Topaz.Service.Shared;

namespace Topaz.Service.Storage.Models;

internal sealed class Filesystem : TopazApiModel
{
    [JsonConstructor]
    public Filesystem()
    {
    }

    public Filesystem(string name, string properties, string encryptionScope)
    {
        Name = name;
        Properties = AsDictionary(properties);
        EncryptionScope = encryptionScope;
        LastModified = DateTimeOffset.UtcNow;
        ETag = new ETag(LastModified.Value.UtcDateTime.Ticks.ToString());
    }

    private static Dictionary<string, string> AsDictionary(string properties)
    {
        if (string.IsNullOrWhiteSpace(properties))
        {
            return [];
        }

        return properties
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(property => property.Split('=', 2))
            .ToDictionary(pair => pair[0], pair => Encoding.UTF8.GetString(Convert.FromBase64String(pair[1])));
    }

    public string ToPropertiesHeader() => string.Join(",", (Properties ?? []).Select(property =>
        $"{property.Key}={Convert.ToBase64String(Encoding.UTF8.GetBytes(property.Value))}"));

    public string? Name { get; init; }
    public string? EncryptionScope { get; init; }
    public Dictionary<string, string>? Properties { get; set; }
    public ETag? ETag { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public bool DeleteScheduled { get; set; }
    public DateTimeOffset? ScheduledDeleteDate { get; set; }

    /// <summary>
    /// Marks the filesystem for deletion by setting the <c>DeleteScheduled</c> property to <c>true</c>
    /// and scheduling the deletion to occur 30 seconds from the current time.
    /// The deletion is represented by updating the <c>ScheduledDeleteDate</c> property.
    /// </summary>
    public void MarkForDeletion()
    {
        DeleteScheduled = true;
        ScheduledDeleteDate = DateTimeOffset.UtcNow.AddSeconds(30);
    }

    public void ClearProperties()
    {
        Properties?.Clear();
    }

    /// <summary>
    /// Updates the filesystem properties by replacing the current property collection with values parsed from a comma-separated string of key-value pairs.
    /// Each property entry uses a name and a base64-encoded value separated by an equals sign.
    /// </summary>
    /// <param name="requestProperties">The comma-separated property string used to populate the filesystem properties.</param>
    public void UpdateProperties(string requestProperties)
    {
        Properties = AsDictionary(requestProperties);
    }
}