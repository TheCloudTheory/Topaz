using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models.Responses;

internal sealed class ApiManagementListProductsResponse : TopazApiModel
{
    public ProductContractResource[] Value { get; init; } = [];
    public uint Count { get; init; }
    public string NextLink { get; set; } = "";

    public static ApiManagementListProductsResponse From(ProductContractResource[] apis)
    {
        return new ApiManagementListProductsResponse
        {
            Value = apis,
            Count = (uint)apis.Length
        };
    }
}