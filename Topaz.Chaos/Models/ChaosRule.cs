using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Chaos.Models;

public sealed class ChaosRule
{
    public required string Id { get; init; }
    public required string ServiceNamespace { get; init; }
    public required FaultType FaultType { get; init; }
    public required double FaultRate { get; init; }
    public int? HttpStatusCode { get; init; }
    public bool Enabled { get; set; } = true;

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
