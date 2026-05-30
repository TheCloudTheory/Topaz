using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Models.Responses;

[UsedImplicitly]
internal sealed class ListKeysEventHubNamespaceResponse
{
    public string? PrimaryConnectionString { get; init; }
    public string? SecondaryConnectionString { get; init; }
    public string? AliasPrimaryConnectionString { get; init; }
    public string? AliasSecondaryConnectionString { get; init; }
    public string? PrimaryKey { get; init; }
    public string? SecondaryKey { get; init; }
    public string? KeyName { get; init; }

    public static ListKeysEventHubNamespaceResponse For(string namespaceName, string authorizationRuleName) => new()
    {
        PrimaryConnectionString = $"Endpoint=sb://{namespaceName}.eventhub.topaz.local.dev:{GlobalSettings.AmqpTlsConnectionPort};SharedAccessKeyName={authorizationRuleName};SharedAccessKey=SAS_KEY_VALUE;",
        SecondaryConnectionString = $"Endpoint=sb://{namespaceName}.eventhub.topaz.local.dev:{GlobalSettings.AmqpTlsConnectionPort};SharedAccessKeyName={authorizationRuleName};SharedAccessKey=SAS_KEY_VALUE;",
        AliasPrimaryConnectionString = null,
        AliasSecondaryConnectionString = null,
        PrimaryKey = "SAS_KEY_VALUE",
        SecondaryKey = "SAS_KEY_VALUE",
        KeyName = authorizationRuleName
    };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
