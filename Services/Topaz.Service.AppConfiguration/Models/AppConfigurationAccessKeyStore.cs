using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class AppConfigurationAccessKeyStore
{
    public List<ConfigurationStoreAccessKey> Keys { get; set; } = [];

    public static AppConfigurationAccessKeyStore Generate(string storeName)
    {
        return new AppConfigurationAccessKeyStore
        {
            Keys =
            [
                ConfigurationStoreAccessKey.Create("Primary", "Primary", false, storeName),
                ConfigurationStoreAccessKey.Create("Secondary", "Secondary", false, storeName),
                ConfigurationStoreAccessKey.Create("Primary Read Only", "Primary Read Only", true, storeName),
                ConfigurationStoreAccessKey.Create("Secondary Read Only", "Secondary Read Only", true, storeName)
            ]
        };
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
