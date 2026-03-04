using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.Entra.Models;

internal sealed class Directory : Entity
{
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}