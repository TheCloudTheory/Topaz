using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Shared;

namespace Topaz.CloudEnvironment.Models.Responses;

internal sealed class ListTenantsResponse
{
    [Obsolete]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ListTenantsResponse()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public ListTenantsResponse(Guid tenantId)
    {
        Value = [new TenantData(tenantId, "Topaz Cloud Environment")];
    }
    
    public TenantData[] Value { get; init; }

    public class TenantData
    {
        [Obsolete]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public TenantData()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
        }

        public TenantData(Guid tenantId, string displayName)
        {
            Id = $"/tenants/{tenantId}";
            TenantId = tenantId;
            DisplayName = displayName;
        }
        
        public string Id { get; init; }
        public Guid TenantId { get; init; }
        public string DisplayName { get; init; }
        public string TenantType => "AAD";
        public string DefaultDomain => "topaz.local.dev";
    }
    
    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}