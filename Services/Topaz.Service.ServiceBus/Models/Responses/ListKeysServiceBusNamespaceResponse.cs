using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Models.Responses;

[UsedImplicitly]
internal sealed class ListKeysServiceBusNamespaceResponse
{
    public string? PrimaryConnectionString { get; init; }
    public string? SecondaryConnectionString { get; init; }
    public string? AliasPrimaryConnectionString { get; init; }
    public string? AliasSecondaryConnectionString { get; init; }
    public string? PrimaryKey { get; init; }
    public string? SecondaryKey { get; init; }
    public string? KeyName { get; init; }

    public static ListKeysServiceBusNamespaceResponse For(string namespaceName, string authorizationRuleName,
        string primaryKey, string secondaryKey) => new()
    {
        PrimaryConnectionString = $"Endpoint=sb://{namespaceName}.servicebus.topaz.local.dev:{GlobalSettings.AmqpTlsConnectionPort};SharedAccessKeyName={authorizationRuleName};SharedAccessKey={primaryKey};",
        SecondaryConnectionString = $"Endpoint=sb://{namespaceName}.servicebus.topaz.local.dev:{GlobalSettings.AmqpTlsConnectionPort};SharedAccessKeyName={authorizationRuleName};SharedAccessKey={secondaryKey};",
        AliasPrimaryConnectionString = null,
        AliasSecondaryConnectionString = null,
        PrimaryKey = primaryKey,
        SecondaryKey = secondaryKey,
        KeyName = authorizationRuleName
    };

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
