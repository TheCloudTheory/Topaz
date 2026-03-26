using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class ListApplicationsResponse
{
    [JsonPropertyName("@odata.context")]
    [UsedImplicitly]
    public string OdataContext
    {
        get;
        set;
    } = "https://graph.microsoft.com/v1.0/$metadata#applications";
    
    [JsonPropertyName("@odata.count")]
    public int? OdataCount
    {
        [UsedImplicitly] get;
        set;
    }
    
    public Application[] Value { [UsedImplicitly] get; init; } = [];

    public static ListApplicationsResponse From(Application[]? applications)
    {
        return new ListApplicationsResponse
        {
            Value = applications ?? []
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}