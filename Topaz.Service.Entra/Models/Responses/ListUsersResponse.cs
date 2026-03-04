using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class ListUsersResponse
{
    [JsonPropertyName("@odata.context")]
    public string OdataContext
    {
        get;
        set;
    } = "https://graph.microsoft.com/v1.0/$metadata#users";
    
    [JsonPropertyName("@odata.count")]
    public int? OdataCount
    {
        get;
        set;
    }
    
    public User[] Value { get; init; } = [];

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