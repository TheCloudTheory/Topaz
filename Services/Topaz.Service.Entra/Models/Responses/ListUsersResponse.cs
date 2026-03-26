using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class ListUsersResponse
{
    [JsonPropertyName("@odata.context")]
    [UsedImplicitly]
    public string OdataContext
    {
        get;
        set;
    } = "https://graph.microsoft.com/v1.0/$metadata#users";
    
    [JsonPropertyName("@odata.count")]
    public int? OdataCount
    {
        [UsedImplicitly] get;
        set;
    }
    
    public User[] Value { [UsedImplicitly] get; init; } = [];

    public static ListUsersResponse From(User[]? users)
    {
        return new ListUsersResponse
        {
            Value = users ?? []
        };
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}