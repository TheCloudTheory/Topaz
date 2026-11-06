using System.Text.Json.Serialization;
using Azure;
using Microsoft.Extensions.Primitives;
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

    private Dictionary<string, string> AsDictionary(string properties)
    {
        var segments = properties.Split(",");
        return segments.Select(segment => segment.Split(":")).ToDictionary(pair => pair[0], pair => pair[1]);
    }

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
    /// Each property entry is expected to use a colon to separate the property name from its value.
    /// </summary>
    /// <param name="requestProperties">The comma-separated property string used to populate the filesystem properties.</param>
    public void UpdateProperties(string requestProperties)
    {
        var segments = requestProperties.Split(",");
        Properties = segments.Select(segment => segment.Split(":")).ToDictionary(pair => pair[0], pair => pair[1]);
    }
}