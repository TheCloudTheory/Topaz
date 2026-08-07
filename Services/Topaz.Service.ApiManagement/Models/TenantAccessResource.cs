using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class TenantAccessResource : ArmSubresource<TenantAccessResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public TenantAccessResource()
#pragma warning restore CS8618
    {
    }

    public TenantAccessResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        TenantAccessResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/tenant/access";
        Name = "signin";
        Properties = properties;
        ETag = ContractEtag.New();
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/tenant";
    public override TenantAccessResourceProperties Properties { get; init; }
    
    [JsonIgnore]
    public ContractEtag? ETag { get; set; }
    
    [JsonIgnore]
    public bool IsDefault { get; set; }

    public static TenantAccessResource Default => new()
    {
        IsDefault = true,
        Properties = new TenantAccessResourceProperties
        {
            Enabled = false
        }
    };

    public static TenantAccessResourceProperties From(CreateOrUpdateTenantAccessRequest request)
    {
        return new TenantAccessResourceProperties
        {
            Enabled = request.Properties?.Enabled ?? false,
            Id = request.Properties?.Id ?? null,
            PrincipalId = request.Properties?.PrincipalId ?? null,
            PrimaryKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            SecondaryKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
        };
    }

    public void UpdateFromRequest(CreateOrUpdateTenantAccessRequest request)
    {
        Properties.Enabled = request.Properties?.Enabled ?? false;
        Properties.Id = request.Properties?.Id ?? Properties.Id;
        Properties.PrincipalId = request.Properties?.PrincipalId ?? Properties.PrincipalId;
    }

    /// <summary>
    /// Removes sensitive information, such as PrimaryKey and SecondaryKey,
    /// from the current instance of the TenantAccessResource to prepare it for retrieval.
    /// </summary>
    /// <returns>
    /// The updated instance of <see cref="TenantAccessResource"/> with the sensitive
    /// information cleared.
    /// </returns>
    public TenantAccessResource ForGetOperation()
    {
        Properties.PrimaryKey = null;
        Properties.SecondaryKey = null;
        return this;
    }
}