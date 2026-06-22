using System.Text.Json;
using Topaz.Shared;

namespace Topaz.CLI.Infrastructure;

public sealed class DefaultsProvider
{
    private static DefaultValuesModel? _defaults;
    static DefaultsProvider()
    {
        InitializeDefaultsIfNeeded();
    }

    private static void InitializeDefaultsIfNeeded()
    {
        if (File.Exists(GlobalSettings.DefaultsPath))
        {
            return;
        }
        
        File.WriteAllText(GlobalSettings.DefaultsPath, new DefaultValuesModel().ToString());
    }

    public void UpdateDefaults(DefaultValuesModel newDefaults)
    {
        var currentDefaults = LoadDefaults();
        _defaults = currentDefaults.UpdateWith(newDefaults);
        File.WriteAllText(GlobalSettings.DefaultsPath,
            JsonSerializer.Serialize(_defaults, GlobalSettings.JsonOptions));
    }

    public DefaultValuesModel LoadDefaults()
    {
        if(_defaults != null) return _defaults;
        var defaults = File.ReadAllText(GlobalSettings.DefaultsPath);
        return JsonSerializer.Deserialize<DefaultValuesModel>(defaults, GlobalSettings.JsonOptions)!;
    }
}