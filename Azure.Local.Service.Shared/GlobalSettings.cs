using System.Text.Json;

namespace Azure.Local.Service.Shared;

public sealed class GlobalSettings
{
    public static JsonSerializerOptions JsonOptions = new()
    {
         PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
