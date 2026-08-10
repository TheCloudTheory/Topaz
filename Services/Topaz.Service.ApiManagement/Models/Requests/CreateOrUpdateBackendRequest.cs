using Azure.ResourceManager.Resources;
using Topaz.Shared;

namespace Topaz.Service.ApiManagement.Models.Requests;

internal sealed class CreateOrUpdateBackendRequest
{
    public BackendContractResourceProperties? Properties { get; set; }

    public static CreateOrUpdateBackendRequest From(BackendContractResource backend)
    {
        return new CreateOrUpdateBackendRequest
        {
            Properties = backend.Properties
        };
    }

    public static CreateOrUpdateBackendRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateBackendRequest
        {
            Properties = data.Properties.ToObjectFromJson<BackendContractResourceProperties>(GlobalSettings.JsonOptions)
        };
    }
}