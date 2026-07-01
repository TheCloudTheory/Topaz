using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.AppConfiguration.Models.Requests;

internal sealed class UpdateConfigurationStoreRequest
{
    public IDictionary<string, string>? Tags { get; set; }
    public ResourceSku? Sku { get; set; }
    public UpdateConfigurationStoreRequestProperties? Properties { get; set; }
}

internal sealed class UpdateConfigurationStoreRequestProperties
{
    public string? PublicNetworkAccess { get; set; }
}
