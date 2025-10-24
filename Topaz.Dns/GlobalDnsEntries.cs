using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Dns;

public record GlobalDnsEntries
{
    public IDictionary<string, List<string>> Entries { get; } = new Dictionary<string, List<string>>();

    public static void AddEntry(string serviceName, string instanceName)
    {
        var file = File.ReadAllText(GlobalSettings.GlobalDnsEntriesFilePath);
        var entries = JsonSerializer.Deserialize<GlobalDnsEntries>(file);

        if (entries == null) throw new InvalidOperationException();
        if (entries.Entries.TryGetValue(serviceName, out var value))
        {
            value.Add(instanceName);
        }
        else
        {
            entries.Entries[serviceName] = [instanceName];
        }
        
        File.WriteAllText(GlobalSettings.GlobalDnsEntriesFilePath, JsonSerializer.Serialize(entries));
    }
}