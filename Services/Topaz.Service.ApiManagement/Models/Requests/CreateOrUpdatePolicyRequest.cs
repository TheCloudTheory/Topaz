namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CreateOrUpdatePolicyRequest
{
    public PolicyContractResourceProperties? Properties { get; init; }

    public static CreateOrUpdatePolicyRequest From(PolicyContractResource backend)
    {
        return new CreateOrUpdatePolicyRequest
        {
            Properties = backend.Properties
        };
    }
}