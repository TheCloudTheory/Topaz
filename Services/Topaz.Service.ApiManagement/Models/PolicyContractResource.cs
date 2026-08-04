using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class PolicyContractResource : ArmSubresource<PolicyContractResourceProperties>, IValidatable
{
    [JsonConstructor]
#pragma warning disable CS8618
    public PolicyContractResource()
#pragma warning restore CS8618
    {
    }

    public PolicyContractResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        string name,
        PolicyContractResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/policies/{name}";
        Name = name;
        Properties = properties;
        ETag = ContractEtag.New();
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/policies";
    public override PolicyContractResourceProperties Properties { get; init; }
    
    [JsonIgnore]
    public ContractEtag? ETag { get; set; }

    public void UpdateFromRequest(CreateOrUpdatePolicyRequest request)
    {
        throw new NotImplementedException();
    }

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        return string.IsNullOrEmpty(Properties.Value) ? (false, "Policy value cannot be null or empty") : (true, null);
    }
}