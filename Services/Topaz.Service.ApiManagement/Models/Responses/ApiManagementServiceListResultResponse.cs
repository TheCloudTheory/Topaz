using Topaz.Service.Shared;

namespace Topaz.Service.ApiManagement.Models.Responses;

internal sealed class ApiManagementServiceListResultResponse : TopazApiModel
{
    public string? NextLink { get; set; }
    public ApiManagementServiceResource[]? Value { get; set; }

    public static ApiManagementServiceListResultResponse From(ApiManagementServiceFullResource[] apims)
    {
        return new ApiManagementServiceListResultResponse
        {
            Value = [.. apims.Select(apim => (ApiManagementServiceResource)apim)]
        };
    }
}