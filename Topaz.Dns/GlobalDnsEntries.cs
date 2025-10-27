using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Dns;

public record GlobalDnsEntries
{
    public IDictionary<string, IDictionary<string, List<string>>> Services { get; init; } = new Dictionary<string, IDictionary<string, List<string>>>();

    public static void AddEntry(string serviceName, Guid subscriptionIdentifier, string? resourceGroupIdentifier, string instanceName)
    {
        var entries = GetDnsEntriesFromFile();

        if (entries == null) throw new InvalidOperationException();
        if (entries.Services.TryGetValue(serviceName, out var value))
        {
            if (value.TryGetValue(GetHierarchyValue(subscriptionIdentifier, resourceGroupIdentifier),
                    out var instances))
            {
                instances.Add(instanceName);
            }
            else
            {
                value[GetHierarchyValue(subscriptionIdentifier, resourceGroupIdentifier)] = [instanceName];
            }
        }
        else
        {
            entries.Services[serviceName] = new  Dictionary<string, List<string>>
            {
                {GetHierarchyValue(subscriptionIdentifier, resourceGroupIdentifier), [instanceName]}
            };
        }
        
        File.WriteAllText(GlobalSettings.GlobalDnsEntriesFilePath, JsonSerializer.Serialize(entries, GlobalSettings.JsonOptionsCli));
    }

    private static GlobalDnsEntries? GetDnsEntriesFromFile()
    {
        var file = File.ReadAllText(GlobalSettings.GlobalDnsEntriesFilePath);
        var entries = JsonSerializer.Deserialize<GlobalDnsEntries>(file, GlobalSettings.JsonOptions);
        return entries;
    }

    private static string GetHierarchyValue(Guid subscriptionIdentifier, string? resourceGroupIdentifier)
    {
        return string.IsNullOrWhiteSpace(resourceGroupIdentifier)
            ? subscriptionIdentifier.ToString()
            : $"{subscriptionIdentifier}:{resourceGroupIdentifier}";
    }

    public static (Guid subscription, string resourceGroup)? GetEntry(string serviceName, string instanceName)
    {
        var entries = GetDnsEntriesFromFile();

        if (entries == null) throw new InvalidOperationException();
        if (!entries.Services.TryGetValue(serviceName, out var globalServiceEntries)) return null;
        
        var existingEntry = globalServiceEntries.SingleOrDefault(entry => entry.Value.Contains(instanceName)).Key;
        if (string.IsNullOrWhiteSpace(existingEntry))
        {
            return null;
        }

        var segments = existingEntry.Split(":");
        return (Guid.Parse(segments[0]), segments.Length > 1 ? segments[1] : null);
    }

    public static void DeleteEntry(string serviceName, Guid subscriptionIdentifier, string? resourceGroupIdentifier,
        string? instanceName)
    {
        var entries = GetDnsEntriesFromFile();
        if (entries == null) throw new InvalidOperationException();
        if (!entries.Services.TryGetValue(serviceName, out var globalServiceEntries)) return;

        globalServiceEntries.Remove(GetHierarchyValue(subscriptionIdentifier, resourceGroupIdentifier));
        
        if (string.IsNullOrWhiteSpace(instanceName) && string.IsNullOrWhiteSpace(resourceGroupIdentifier))
        {
            // If both the name of an instance and a resource group are null,
            // it means a subscription was removed. Cascade delete all the resources
            // which may have entries related to the subscription
            foreach (var service in entries.Services)
            {
                foreach (var instance in service.Value)
                {
                    if (instance.Key.Contains(subscriptionIdentifier.ToString()))
                    {
                        entries.Services.Remove(service.Key);
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(instanceName) && !string.IsNullOrWhiteSpace(resourceGroupIdentifier))
        {
            // If only instance name is null then we need to remove global entries
            // for resources inside that resource group only
            foreach (var service in entries.Services)
            {
                if (service.Key == GetHierarchyValue(subscriptionIdentifier, resourceGroupIdentifier))
                {
                    entries.Services.Remove(service.Key);
                }
            }
        }
            
        var newEntries = JsonSerializer.Serialize(entries, GlobalSettings.JsonOptionsCli);
        File.WriteAllText(GlobalSettings.GlobalDnsEntriesFilePath, newEntries);
    }
}