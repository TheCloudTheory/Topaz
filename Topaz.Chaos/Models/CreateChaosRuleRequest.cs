namespace Topaz.Chaos.Models;

internal sealed class CreateChaosRuleRequest
{
    public required string ServiceNamespace { get; init; }
    public required FaultType FaultType { get; init; }
    public required double FaultRate { get; init; }
    public int? HttpStatusCode { get; init; }
}
