using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Dns;

public record GlobalDnsEntries
{
    public IDictionary<string, IDictionary<string, List<string>>> Entries { get; set; } = new Dictionary<string, IDictionary<string, List<string>>>();

    public static void AddEntry(string serviceName, Guid subscriptionIdentifier, string? resourceGroupIdentifier, string instanceName)
    {
        var entries = GetDnsEntriesFromFile();

        if (entries == null) throw new InvalidOperationException();
        if (entries.Entries.TryGetValue(serviceName, out var value))
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
            entries.Entries[serviceName] = new  Dictionary<string, List<string>>
            {
                {GetHierarchyValue(subscriptionIdentifier, resourceGroupIdentifier), [instanceName]}
            };
        }
        
        File.WriteAllText(GlobalSettings.GlobalDnsEntriesFilePath, JsonSerializer.Serialize(entries));
    }

    private static GlobalDnsEntries? GetDnsEntriesFromFile()
    {
        var file = File.ReadAllText(GlobalSettings.GlobalDnsEntriesFilePath);
        var entries = JsonSerializer.Deserialize<GlobalDnsEntries>(file);
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
        if (!entries.Entries.TryGetValue(serviceName, out var globalServiceEntries)) return null;
        
        var existingEntry = globalServiceEntries.SingleOrDefault(entry => entry.Value.Contains(instanceName)).Key;
        if (string.IsNullOrWhiteSpace(existingEntry))
        {
            return null;
        }

        var segments = existingEntry.Split(":");
        return (Guid.Parse(segments[0]), segments.Length > 1 ? segments[1] : null);
    }

    public static void DeleteEntry(string serviceName, Guid subscriptionIdentifier, string? resourceGroupIdentifier,
        string instanceName)
    {
        var entries = GetDnsEntriesFromFile();
        if (entries == null) throw new InvalidOperationException();
        if (!entries.Entries.TryGetValue(serviceName, out var globalServiceEntries)) return;
        
        globalServiceEntries.Remove(GetHierarchyValue(subscriptionIdentifier, resourceGroupIdentifier));
            
        var newEntries = JsonSerializer.Serialize(entries);
        File.WriteAllText(GlobalSettings.GlobalDnsEntriesFilePath, newEntries);
    }
}