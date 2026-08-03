using Azure;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class ContractEtag
{
    public string? Value { get; init; }

    public static ContractEtag New()
    {
        return new ContractEtag
        {
            Value = new ETag(DateTimeOffset.Now.Ticks.ToString()).ToString()
        };
    }

    public bool IsEqualToETag(string etag)
    {
        return $"\"{Value}\"" == etag;
    }
}