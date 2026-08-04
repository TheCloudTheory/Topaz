using Topaz.Service.ApiManagement.Models.Requests;

namespace Topaz.Service.ApiManagement.Models;

internal sealed class PolicyContractResourceProperties
{
    public string? Format { get; init; } = "xml";
    public string? Value { get; init; }

    public static PolicyContractResourceProperties From(CreateOrUpdatePolicyRequest request)
    {
        return new PolicyContractResourceProperties
        {
            Format = request.Properties?.Format ?? "xml",
            Value = request.Properties?.Value
        };
    }
}