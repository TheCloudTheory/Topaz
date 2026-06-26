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
        
        var rules = ChaosRulesProvider.ListOrdered();
        foreach (var rule in rules)
        {
            logger.LogDebug(nameof(ChaosProvider), nameof(GetChaosResponse), $"Checking chaos rule '{rule.Id}'.");
            
            // For each rule, calculate the current fail chance. The formula is simple:
            // - get a random double between 0 and 1
            // - subtract the fail chance from 1
            // - compare the result with the fault rate of a rule
            // 
            // For example, if the fail chance is 0.05, and the fault rate is 0.1,
            // the rule will be triggered because fail chance < fault rate.
            var failValue = 1 - new Random().NextDouble();
            if (!(failValue < rule.FaultRate) || rule.ServiceNamespace != providerNamespace) continue;
            
            logger.LogDebug(nameof(ChaosProvider), nameof(GetChaosResponse), $"Chaos rule '{rule.Id}' triggered.");
            return (true, await rule.GetResponse());
        }
        
        logger.LogDebug(nameof(ChaosProvider), nameof(GetChaosResponse), "No chaos rule triggered.");
        // No rule triggered, return false
        return (false, null);
    }
}