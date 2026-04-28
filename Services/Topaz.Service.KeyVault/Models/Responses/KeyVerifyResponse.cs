using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class KeyVerifyResponse
{
    public bool Value { get; init; }

    public static KeyVerifyResponse New(bool result) => new() { Value = result };

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
