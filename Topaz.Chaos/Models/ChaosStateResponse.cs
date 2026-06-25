using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Chaos.Models;

internal sealed class ChaosStateResponse
{
    public bool Enabled { get; init; }
    public object[] Rules { get; init; } = [];

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}