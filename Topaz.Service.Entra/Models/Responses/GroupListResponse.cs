using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class ListGroupsResponse
{
    public const string OdataContext = "https://graph.microsoft.com/v1.0/$metadata#groups";
    
    public Group[] Value { get; init; } = [];

    public static ListGroupsResponse From(Group[] groups)
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