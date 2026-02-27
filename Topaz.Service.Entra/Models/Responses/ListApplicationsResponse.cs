using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models.Responses;

internal sealed class ListApplicationsResponse
{
    public const string OdataContext = "https://graph.microsoft.com/v1.0/$metadata#applications";
    
    public Application[] Value { get; init; } = [];

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