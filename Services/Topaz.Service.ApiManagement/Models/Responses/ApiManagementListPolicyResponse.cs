using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models.Responses;

internal sealed class ApiManagementListPolicyResponse : TopazApiModel
{
    public PolicyContractResource[] Value { get; init; } = [];
    public uint Count { get; init; }
    public string NextLink { get; set; } = "";

    public static ApiManagementListPolicyResponse From(PolicyContractResource[] policies)
    {
        return new ApiManagementListPolicyResponse
        {
            Value = policies,
            Count = (uint)policies.Length
        };
    }
}