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

    public Filesystem(string properties, string encryptionScope)
    {
        Properties = properties;
        EncryptionScope = encryptionScope;
        ETag = new ETag(DateTime.UtcNow.Ticks.ToString());
    }

    public string? EncryptionScope { get; init; }
    public string? Properties { get; init; }
    public ETag? ETag { get; init; }
}