using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Endpoints.StorageAccount;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class AzureStorageService(ITopazLogger logger) : IServiceDefinition
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".azure-storage");
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "storage";

    public string Name => "Azure Storage";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new CreateOrUpdateStorageAccountEndpoint(logger),
        new UpdateStorageAccountEndpoint(logger),
        new GetStorageAccountEndpoint(logger),
        new DeleteStorageAccountEndpoint(logger),
        new CheckStorageAccountNameAvailabilityEndpoint(logger),
        new ListStorageAccountKeysEndpoint(logger),
        new RegenerateStorageAccountKeyEndpoint(logger),
        new ListAccountSasEndpoint(logger),
        new ListServiceSasEndpoint(logger),
        new ListStorageAccountsEndpoint(logger),
        new ListStorageAccountsBySubscriptionEndpoint(logger),
        new GetFileServicesDefaultEndpoint(logger),
        new GetBlobServicesDefaultEndpoint(logger),
        new GetTableServicesDefaultEndpoint(logger),
        new GetQueueServicesDefaultEndpoint(logger),
        new CreateOrUpdateArmTableEndpoint(logger),
        new GetArmTableEndpoint(logger),
        new DeleteArmTableEndpoint(logger)
    ];

    public void Bootstrap()
    {
    }
}
