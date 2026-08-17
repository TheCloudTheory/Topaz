using JetBrains.Annotations;
using Topaz.ResourceManager;

namespace Topaz.Service.AppConfiguration.Models.Requests;

public sealed class UpdateConfigurationStoreRequest
{
    public IDictionary<string, string>? Tags { get; init; }
    public ResourceSku? Sku { get; init; }
    public UpdateConfigurationStoreRequestProperties? Properties { get; init; }
}

[UsedImplicitly]
public sealed class UpdateConfigurationStoreRequestProperties
{
    public string? PublicNetworkAccess { get; set; }
    public bool? DisableLocalAuth  { get; set; }
    public bool? EnablePurgeProtection { get; set; }
}
