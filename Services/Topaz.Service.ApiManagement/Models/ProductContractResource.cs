using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ProductContractResource : ArmSubresource<ProductContractResourceProperties>, IValidatable
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ProductContractResource()
#pragma warning restore CS8618
    {
    }

    public ProductContractResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        string name,
        ProductContractResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/products/{name}";
        Name = name;
        Properties = properties;
        ETag = ContractEtag.New();
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/products";
    public override ProductContractResourceProperties Properties { get; init; }
    
    [JsonIgnore]
    public ContractEtag? ETag { get; set; }

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (Name.Length is < 1 or > 256)
        {
            return (false, "Name must be between 1 and 256 characters");
        }

        if (Properties.Description?.Length > 1000)
        {
            return (false, "Description must be less than 1000 characters");
        }
        
        return Properties.DisplayName?.Length > 300 ? (false, "DisplayName must be less than 300 characters") : (true, null);
    }

    public void UpdateFromRequest(CreateOrUpdateProductRequest request)
    {
        Properties.DisplayName = request.Properties?.DisplayName ?? Properties.DisplayName;
        Properties.ApprovalNeeded = request.Properties?.ApprovalNeeded ?? Properties.ApprovalNeeded;
        Properties.Description = request.Properties?.Description ?? Properties.Description;
        Properties.SubscriptionRequired = request.Properties?.SubscriptionRequired ?? Properties.SubscriptionRequired;
        Properties.SubscriptionLimits = request.Properties?.SubscriptionLimits ?? Properties.SubscriptionLimits;
        Properties.Terms = request.Properties?.Terms ?? Properties.Terms;
        Properties.State = request.Properties?.State ?? Properties.State;

        ETag = ContractEtag.New();
    }
    
    public override string GetParentId()
    {
        var segments = Id.Split("/");
        return segments[9];
    }
}