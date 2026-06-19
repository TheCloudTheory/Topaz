using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Service.CosmosDb.Models;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Models;

public sealed class AccountPropertiesResponse
{
    /// <summary>Serialized default query engine configuration as a JSON string.</summary>
    private static readonly string DefaultQueryEngineConfigurationJson =
        JsonSerializer.Serialize(new QueryEngineConfiguration(), GlobalSettings.JsonOptions);

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("_self")]
    public string Self { get; init; } = string.Empty;

    [JsonPropertyName("_rid")]
    public string Rid { get; init; } = string.Empty;

    [JsonPropertyName("writableLocations")]
    public DatabaseAccountLocation[] WriteLocations { get; init; } = [];

    [JsonPropertyName("readableLocations")]
    public DatabaseAccountLocation[] ReadLocations { get; init; } = [];

    [JsonPropertyName("enableMultipleWriteLocations")]
    public bool EnableMultipleWriteLocations { get; init; }

    [JsonPropertyName("consistencyPolicy")]
    public ConsistencyPolicySettings ConsistencyPolicy { get; init; } = new();

    [JsonPropertyName("_type")]
    public string Type { get; init; } = "Database Accounts";

    [JsonPropertyName("userConsistencyPolicy")]
    public ConsistencyPolicySettings UserConsistencyPolicy => ConsistencyPolicy;

    [JsonPropertyName("systemTopologies")]
    public object[] SystemTopologies { get; init; } = [];

    [JsonPropertyName("enableFreeTier")]
    public bool EnableFreeTier { get; init; }

    [JsonPropertyName("documentEndpoint")]
    public string DocumentEndpoint { get; init; } = string.Empty;

    [JsonPropertyName("addressesLink")]
    public string AddressesLink { get; init; } = "//addresses/";

    [JsonPropertyName("_etag")]
    public string Etag { get; init; } = string.Empty;

    /// <summary>
    /// JSON-stringified query engine configuration returned by the Cosmos DB service.
    /// The SDK's QueryPartitionProvider reads this field during initialization; without it
    /// the internal lock is never set and the finalizer crashes with ArgumentNullException.
    /// </summary>
    [JsonPropertyName("queryEngineConfiguration")]
    public string QueryEngineConfiguration { get; init; } = DefaultQueryEngineConfigurationJson;

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
