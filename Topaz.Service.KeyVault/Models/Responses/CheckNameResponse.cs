using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public sealed class CheckNameResponse
{
    public bool NameAvailable { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}