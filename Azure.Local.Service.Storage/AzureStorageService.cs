using Azure.Local.Service.Shared;

namespace Azure.Local.Service.Storage;

public class AzureStorageService : IServiceDefinition
{
    public string Name => "Azure Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new TableEndpoint()
    ];
}
