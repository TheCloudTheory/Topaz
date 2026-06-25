using System.Text.Json;
using Topaz.Chaos.Models;
using Topaz.Shared;

namespace Topaz.Chaos;

internal sealed class ChaosRulesProvider
{
    private static readonly string _filePath =
        Path.Combine(GlobalSettings.MainEmulatorDirectory, "chaos", "rules.json");

    private static readonly Dictionary<string, ChaosRule> _rules;
    private static readonly List<string> _order;
    private static readonly Lock _lock = new();

    static ChaosRulesProvider()
    {
        (_rules, _order) = Load();
    }

    public static bool TryAdd(ChaosRule rule)
    {
        lock (_lock)
        {
            if (_rules.ContainsKey(rule.Id)) return false;
            _rules[rule.Id] = rule;
            _order.Add(rule.Id);
            Save();
            return true;
        }
    }

    public static bool TryGet(string id, out ChaosRule? rule)
    {
        lock (_lock) return _rules.TryGetValue(id, out rule);
    }

    public static bool TryDelete(string id)
    {
        lock (_lock)
        {
            if (!_rules.Remove(id)) return false;
            _order.Remove(id);
            Save();
            return true;
        }
    }

    public static IReadOnlyList<ChaosRule> ListOrdered()
    {
        lock (_lock) return _order.Select(id => _rules[id]).ToList();
    }

    public static bool SetEnabled(string id, bool enabled)
    {
        lock (_lock)
        {
            if (!_rules.TryGetValue(id, out var rule)) return false;
            rule.Enabled = enabled;
            Save();
            return true;
        }
    }

    // --- persistence ---

    private static (Dictionary<string, ChaosRule>, List<string>) Load()
    {
        if (!File.Exists(_filePath))
            return (new Dictionary<string, ChaosRule>(StringComparer.OrdinalIgnoreCase), []);

        var json = File.ReadAllText(_filePath);
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
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var ordered = _order.Select(id => _rules[id]).ToList();
        File.WriteAllText(_filePath, JsonSerializer.Serialize(ordered, GlobalSettings.JsonOptions));
    }
}
