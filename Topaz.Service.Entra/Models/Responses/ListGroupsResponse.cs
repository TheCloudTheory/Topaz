using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class ListGroupsResponse
{
    [JsonPropertyName("@odata.context")]
    public string OdataContext
    {
        get;
        set;
    } = "https://graph.microsoft.com/v1.0/$metadata#groups";
    
    [JsonPropertyName("@odata.count")]
    public int? OdataCount
    {
        get;
        set;
    }
    
    public Group[] Value { [UsedImplicitly] get; init; } = [];

    public static ListGroupsResponse From(Group[]? groups)
    {
        return new ListGroupsResponse
        {
            Value = groups ?? []
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}