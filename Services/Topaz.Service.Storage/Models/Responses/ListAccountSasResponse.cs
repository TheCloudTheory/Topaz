using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models.Responses;

internal sealed class ListAccountSasResponse(string accountSasToken)
{
    [UsedImplicitly] public string AccountSasToken { get; init; } = accountSasToken;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
