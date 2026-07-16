using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Shared;

public abstract class TopazApiModel
{
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GetType(), GlobalSettings.JsonOptions);
    }
}