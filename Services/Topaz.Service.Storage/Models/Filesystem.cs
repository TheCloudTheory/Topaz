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
        Properties = properties;
        EncryptionScope = encryptionScope;
        LastModified = DateTimeOffset.UtcNow;
        ETag = new ETag(LastModified.Value.UtcDateTime.Ticks.ToString());
    }

    public string? Name { get; init; }
    public string? EncryptionScope { get; init; }
    public string? Properties { get; init; }
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
}