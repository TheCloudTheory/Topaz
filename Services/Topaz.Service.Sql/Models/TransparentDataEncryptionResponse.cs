using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Sql.Models;

public sealed class TransparentDataEncryptionResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "Microsoft.Sql/servers/databases/transparentDataEncryption";
    public TransparentDataEncryptionProperties Properties { get; init; } = new();

    public static TransparentDataEncryptionResponse ForDatabase(
        string subscriptionId,
        string resourceGroupName,
        string serverName,
        string databaseName)
    {
        return new TransparentDataEncryptionResponse
        {
            Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}" +
                 $"/providers/Microsoft.Sql/servers/{serverName}/databases/{databaseName}" +
                 "/transparentDataEncryption/current",
            Name = "current",
            Properties = new TransparentDataEncryptionProperties { State = "Enabled" }
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}

public sealed class TransparentDataEncryptionProperties
{
    public string State { get; init; } = "Enabled";
}
