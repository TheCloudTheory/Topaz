using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ResourceGroup.Models.Responses;

internal sealed record ListResourceGroupsResponse
{
    [Obsolete]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ListResourceGroupsResponse()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public ListResourceGroupsResponse(IEnumerable<ResourceGroupResource> resourceGroups)
    {
        Value = resourceGroups.ToArray();
    }
    
    public ResourceGroupResource[] Value { get; init; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}