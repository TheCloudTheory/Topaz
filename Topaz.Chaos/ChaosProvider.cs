using Topaz.Shared;

namespace Topaz.Chaos;

public sealed class ChaosProvider(ITopazLogger logger)
{
    public async Task<(bool isFaulted, HttpResponseMessage? response)> GetChaosResponse(string? providerNamespace)
    {
        if (!ChaosStateProvider.IsEnabled)
        {
            logger.LogDebug(nameof(ChaosProvider), nameof(GetChaosResponse), "Chaos is not enabled.");
            return (false, null);
        }

        if (providerNamespace == "Topaz")
        {
            logger.LogDebug(nameof(ChaosProvider), nameof(GetChaosResponse), "Chaos is not applicable to Topaz endpoints.");
            return (false, null);
        }
        
        var rules = ChaosRulesProvider.ListOrdered();
        foreach (var rule in rules)
        {
            logger.LogDebug(nameof(ChaosProvider), nameof(GetChaosResponse), $"Checking chaos rule '{rule.Id}'.");

            if (!rule.Enabled) continue;

            var namespaceMatches = rule.ServiceNamespace == "*" || rule.ServiceNamespace == providerNamespace;
            if (!namespaceMatches) continue;

            // Roll the fault: 1 - NextDouble() gives a value in (0, 1].
            // Inject the fault when the rolled value falls below FaultRate.
            var failValue = 1 - new Random().NextDouble();
            if (failValue >= rule.FaultRate) continue;

            logger.LogInformation($"[ChaosProvider] Rule '{rule.Id}' triggered (faultType={rule.FaultType}, faultRate={rule.FaultRate}).");
            return (true, await rule.GetResponse());
        }
        
        logger.LogDebug(nameof(ChaosProvider), nameof(GetChaosResponse), "No chaos rule triggered.");
        // No rule triggered, return false
        return (false, null);
    }
}