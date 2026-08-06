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
}