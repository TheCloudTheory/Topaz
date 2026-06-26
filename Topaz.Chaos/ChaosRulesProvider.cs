using System.Text.Json;
using Topaz.Chaos.Models;
using Topaz.Shared;

namespace Topaz.Chaos;

internal sealed class ChaosRulesProvider
{
    private static readonly string FilePath =
        Path.Combine(GlobalSettings.MainEmulatorDirectory, "chaos", "rules.json");

    private static readonly Dictionary<string, ChaosRule> Rules;
    private static readonly List<string> Order;
    private static readonly Lock Lock = new();

    static ChaosRulesProvider()
    {
        (Rules, Order) = Load();
    }

    public static bool TryAdd(ChaosRule rule)
    {
        lock (Lock)
        {
            if (!Rules.TryAdd(rule.Id, rule)) return false;
            Order.Add(rule.Id);
            Save();
            return true;
        }
    }

    public static bool TryGet(string id, out ChaosRule? rule)
    {
        lock (Lock) return Rules.TryGetValue(id, out rule);
    }

    public static bool TryDelete(string id)
    {
        lock (Lock)
        {
            if (!Rules.Remove(id)) return false;
            Order.Remove(id);
            Save();
            return true;
        }
    }

    public static IReadOnlyList<ChaosRule> ListOrdered()
    {
        lock (Lock) return Order.Select(id => Rules[id]).ToList();
    }

    public static bool SetEnabled(string id, bool enabled)
    {
        lock (Lock)
        {
            if (!Rules.TryGetValue(id, out var rule)) return false;
            rule.Enabled = enabled;
            Save();
            return true;
        }
    }

    private static (Dictionary<string, ChaosRule>, List<string>) Load()
    {
        if (!File.Exists(FilePath))
            return (new Dictionary<string, ChaosRule>(StringComparer.OrdinalIgnoreCase), []);

        var json = File.ReadAllText(FilePath);
        var rules = JsonSerializer.Deserialize<List<ChaosRule>>(json, GlobalSettings.JsonOptions) ?? [];
        var dict = new Dictionary<string, ChaosRule>(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();
        foreach (var rule in rules)
        {
            dict[rule.Id] = rule;
            order.Add(rule.Id);
        }
        return (dict, order);
    }

    private static void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var ordered = Order.Select(id => Rules[id]).ToList();
        File.WriteAllText(FilePath, JsonSerializer.Serialize(ordered, GlobalSettings.JsonOptions));
    }
}
