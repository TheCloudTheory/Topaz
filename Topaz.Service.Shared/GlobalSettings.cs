using System.Text.Json;

namespace Topaz.Service.Shared;

public sealed class GlobalSettings
{
    public static JsonSerializerOptions JsonOptions = new()
    {
         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
         PropertyNameCaseInsensitive = true
    };
}
