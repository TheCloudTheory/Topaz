using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models.Responses;

internal sealed class ListKeysResponse(TopazStorageAccountKey[] keys)
{
    [UsedImplicitly] public TopazStorageAccountKey[] Keys { get; init; } = keys;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}