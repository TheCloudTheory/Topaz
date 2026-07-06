using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Models;

public record TenantMetadata
{
    public string Id { get; init; }
    public string TenantId { get; init; }
    public string DisplayName { get; init; }
    public string CountryCode { get; init; }
    public string[] Domains { get; init; }

    public TenantMetadata()
    {
        TenantId = GlobalSettings.DefaultTenantId;
        Id = $"/tenants/{TenantId}";
        DisplayName = "Topaz";
        CountryCode = "US";
        Domains = [];
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
