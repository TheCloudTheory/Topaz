using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class ReplicaResourceProperties
{
    public string? Endpoint { get; init; }
    public string ProvisioningState { get; init; } = "Succeeded";

    public static ReplicaResourceProperties From(string replicaName, ConfigurationStoreFullResource store)
    {
        return new ReplicaResourceProperties
        {
            Endpoint =
                $"https://{store.Name}-{replicaName}.{GlobalSettings.AppConfigurationDnsSuffix}:{GlobalSettings.DefaultAppConfigurationPort}/"
        };
    }
}