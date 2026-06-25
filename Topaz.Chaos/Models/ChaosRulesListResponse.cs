using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Chaos.Models;

internal sealed class ChaosRulesListResponse
{
    public required IReadOnlyList<ChaosRule> Value { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
