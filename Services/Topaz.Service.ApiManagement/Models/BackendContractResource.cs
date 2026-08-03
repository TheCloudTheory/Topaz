using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.ApiManagement.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class BackendContractResource : ArmSubresource<BackendContractResourceProperties>, IValidatable
{
    [JsonConstructor]
#pragma warning disable CS8618
    public BackendContractResource()
#pragma warning restore CS8618
    {
    }

    public BackendContractResource(
        SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string parentName,
        string name,
        BackendContractResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{parentName}/backends/{name}";
        Name = name;
        Properties = properties;
        ETag = ContractEtag.New();
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.ApiManagement/service/backends";
    public override BackendContractResourceProperties Properties { get; init; }
    
    [JsonIgnore]
    public ContractEtag? ETag { get; set; }

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (Properties.Description?.Length > 2000)
        {
            return (false, "Description must be less than 2000 characters");
        }
        
        if(Properties.ResourceId?.Length > 2000)
        {
            return (false, "ResourceId must be less than 2000 characters");
        }

        if (Properties.Title?.Length > 300)
        {
            return (false, "Title must be less than 300 characters");
        }
        
        if(Properties.Url?.Length > 2000)
        {
            return (false, "Url must be less than 2000 characters");
        }
        
        return (true, null);
    }

    public void UpdateFromRequest(CreateOrUpdateBackendRequest request)
    {
        Properties.Properties = request.Properties?.Properties ?? Properties.Properties;
        Properties.Pool = request.Properties?.Pool ?? Properties.Pool;
        Properties.CircuitBreaker = request.Properties?.CircuitBreaker ?? Properties.CircuitBreaker;
        Properties.Proxy = request.Properties?.Proxy ?? Properties.Proxy;
        Properties.Tls = request.Properties?.Tls ?? Properties.Tls;
        Properties.Credentials = request.Properties?.Credentials ?? Properties.Credentials;
        Properties.Type = request.Properties?.Type ?? Properties.Type;
        Properties.Url = request.Properties?.Url ?? Properties.Url;
        Properties.ResourceId = request.Properties?.ResourceId ?? Properties.ResourceId;
        Properties.Title = request.Properties?.Title ?? Properties.Title;
        Properties.Protocol = request.Properties?.Protocol ?? Properties.Protocol;
        Properties.Description = request.Properties?.Description ?? Properties.Description;
    }
}