using Azure.ResourceManager.Resources;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CreateOrUpdateProductRequest
{
    public ProductContractResourceProperties? Properties { get; set; }

    public static CreateOrUpdateProductRequest From(ProductContractResource product)
    {
        return new CreateOrUpdateProductRequest
        {
            Properties = product.Properties
        };
    }

    public static CreateOrUpdateProductRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateProductRequest
        {
            Properties = data.Properties.ToObjectFromJson<ProductContractResourceProperties>(GlobalSettings.JsonOptions)
        };
    }
}