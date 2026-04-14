using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models.Responses;

internal sealed class ListServiceSasResponse(string serviceSasToken)
{
    [UsedImplicitly] public string ServiceSasToken { get; init; } = serviceSasToken;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
