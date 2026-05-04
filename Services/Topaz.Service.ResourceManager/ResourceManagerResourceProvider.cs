using System.Text.Json;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerResourceProvider(ITopazLogger logger) : ResourceProviderBase<ResourceManagerService>(logger)
{
    private static readonly HashSet<string> DefaultRegisteredNamespaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.Resources",
        "Microsoft.Authorization",
        "Microsoft.Storage",
        "Microsoft.KeyVault",
        "Microsoft.ManagedIdentity",
        "Microsoft.Network",
        "Microsoft.ServiceBus",
        "Microsoft.EventHub",
        "Microsoft.Insights",
        "Microsoft.ContainerRegistry",
        "Microsoft.Compute",
    };

    private static string GetRegistrationsFilePath(Guid subscriptionId) =>
        Path.Combine(BaseEmulatorPath, ".subscription", subscriptionId.ToString(), "provider-registrations.json");

    public string GetProviderRegistrationState(Guid subscriptionId, string providerNamespace)
    {
        var path = GetRegistrationsFilePath(subscriptionId);
        if (File.Exists(path))
        {
            var raw = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(raw, GlobalSettings.JsonOptions);
            if (dict != null && dict.TryGetValue(providerNamespace, out var state))
                return state;
        }

        return DefaultRegisteredNamespaces.Contains(providerNamespace)
            ? ResourceProviderDataResponse.RegisteredState
            : ResourceProviderDataResponse.NotRegisteredState;
    }

    public void SetProviderRegistrationState(Guid subscriptionId, string providerNamespace, string state)
    {
        var path = GetRegistrationsFilePath(subscriptionId);
        Dictionary<string, string> dict;

        if (File.Exists(path))
        {
            var raw = File.ReadAllText(path);
            dict = JsonSerializer.Deserialize<Dictionary<string, string>>(raw, GlobalSettings.JsonOptions)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        dict[providerNamespace] = state;
        File.WriteAllText(path, JsonSerializer.Serialize(dict, GlobalSettings.JsonOptions));
    }
}