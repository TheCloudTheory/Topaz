using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Models;

public sealed class ConfigurationStoreAccessKey
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? ConnectionString { get; set; }
    public DateTime LastModified { get; set; }
    public bool ReadOnly { get; set; }

    public static ConfigurationStoreAccessKey Create(string id, string name, bool readOnly, string storeName)
    {
        var value = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        return new ConfigurationStoreAccessKey
        {
            Id = id,
            Name = name,
            Value = value,
            ConnectionString = $"Endpoint=https://{storeName}.azconfig.topaz.local.dev:{GlobalSettings.DefaultAppConfigurationPort}/;Id={id};Secret={value}",
            ReadOnly = readOnly,
            LastModified = DateTime.UtcNow
        };
    }

    public void Regenerate(string storeName)
    {
        var newValue = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        Value = newValue;
        ConnectionString = $"Endpoint=https://{storeName}.azconfig.topaz.local.dev:{GlobalSettings.DefaultAppConfigurationPort}/;Id={Id};Secret={newValue}";
        LastModified = DateTime.UtcNow;
    }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
