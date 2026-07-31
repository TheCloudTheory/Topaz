using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiContractResource : ArmSubresource<ApiContractResourceProperties>, IValidatable
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ApiContractResource()
#pragma warning restore CS8618
    {
    }

    public ApiContractResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        string name,
        ApiContractResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/apis/{name}";
        Name = name;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/apis";
    public override ApiContractResourceProperties Properties { get; init; }

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        return (true, null);
    }

    public void UpdateFromRequest(CreateOrUpdateApiRequest request)
    {
        throw new NotImplementedException();
    }
}