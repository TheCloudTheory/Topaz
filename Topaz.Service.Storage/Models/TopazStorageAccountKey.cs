using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.Storage.Models;

internal sealed class TopazStorageAccountKey(
    string keyName,
    string value,
    string? permissions,
    DateTimeOffset? createdOn)
{
    [UsedImplicitly] public string KeyName { get; } = keyName;
    [UsedImplicitly] public string Value { get; } = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value)));
    [UsedImplicitly] public string? Permissions { get; } = permissions;
    
    [JsonPropertyName("creationTime")]
    [UsedImplicitly] public DateTimeOffset? CreatedOn { get; } = createdOn;
}