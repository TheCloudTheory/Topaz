using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models.Responses;

internal sealed class ApiManagementListApiRevisionsResponse : TopazApiModel
{
    public ApiRevisionData[] Value { get; init; } = [];
    public uint Count { get; init; }
    public string NextLink { get; init; } = string.Empty;

    public static ApiManagementListApiRevisionsResponse From(ApiRevisionData[] revisions)
    {
        return new ApiManagementListApiRevisionsResponse
        {
            Value = revisions,
            Count = (uint)revisions.Length
        };
    }
}