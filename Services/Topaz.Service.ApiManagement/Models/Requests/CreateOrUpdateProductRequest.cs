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
}