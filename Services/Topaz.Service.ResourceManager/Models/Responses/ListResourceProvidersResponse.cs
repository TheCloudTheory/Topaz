using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models.Responses;

public sealed class ListResourceProvidersResponse
{
    public ResourceProviderDataResponse[] Value { get; init; } = [];

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}