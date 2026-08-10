using Azure.ResourceManager.Resources;
using Topaz.Shared;

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

    public static CreateOrUpdatePolicyRequest From(GenericResourceData data)
    {
        return new CreateOrUpdatePolicyRequest
        {
            Properties = data.Properties.ToObjectFromJson<PolicyContractResourceProperties>(GlobalSettings.JsonOptions)
        };
    }
}