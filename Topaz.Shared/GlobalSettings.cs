using System.Text.Json;

namespace Topaz.Shared;

public static class GlobalSettings
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
         PropertyNameCaseInsensitive = true
    };
}
