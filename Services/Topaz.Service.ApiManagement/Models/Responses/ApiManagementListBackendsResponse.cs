using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models.Responses;

internal sealed class ApiManagementListBackendsResponse : TopazApiModel
{
    public BackendContractResource[] Value { get; init; } = [];
    public uint Count { get; init; }
    public string NextLink { get; set; } = "";

    public static ApiManagementListBackendsResponse From(BackendContractResource[] backends)
    {
        return new ApiManagementListBackendsResponse
        {
            Value = backends,
            Count = (uint)backends.Length
        };
    }
}