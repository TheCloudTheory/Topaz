using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Shared.Models;

public abstract class TopazApiRequest
{
    public static T? Deserialize<T>(string content)
    {
        return JsonSerializer.Deserialize<T>(content, GlobalSettings.JsonOptions);
    }
}