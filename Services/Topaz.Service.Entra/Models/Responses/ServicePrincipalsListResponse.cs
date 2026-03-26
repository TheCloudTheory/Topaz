using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class ServicePrincipalsListResponse
{
    [JsonPropertyName("@odata.context")]
    public string? OdataContext => "https://graph.microsoft.com/v1.0/$metadata#servicePrincipals";

    [JsonPropertyName("@odata.nextLink")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OdataNextLink { get; init; }

    [JsonPropertyName("@odata.count")] public int? OdataCount => Value.Length;

    public ServicePrincipal[] Value { get; init; } = [];

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    public static ServicePrincipalsListResponse From(Models.ServicePrincipal[] servicePrincipals)
    {
        return new ServicePrincipalsListResponse
        {
            Value = servicePrincipals
        };
    }
}
