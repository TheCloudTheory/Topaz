using Azure;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ApiContractEtag
{
    public string? Value { get; init; }

    public static ApiContractEtag New()
    {
        return new ApiContractEtag
        {
            Value = new ETag(DateTimeOffset.Now.Ticks.ToString()).ToString()
        };
    }
}