using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models.Responses;

internal sealed class ApiManagementListApisResponse : TopazApiModel
{
    public ApiContractResource[] Value { get; init; } = [];
    public uint Count { get; init; }
    public string NextLink { get; set; } = "";

    public static ApiManagementListApisResponse From(ApiContractResource[] apis)
    {
        return new ApiManagementListApisResponse
        {
            Value = apis,
            Count = (uint)apis.Length
        };
    }
}