using Topaz.EventPipeline;
using Topaz.EventPipeline.Events;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Endpoints.StorageAccount;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.Storage.Services;

public sealed class AzureStorageService(Pipeline eventPipeline, ITopazLogger logger) : IServiceDefinition
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
        new GetFileServicesDefaultEndpoint(),
        new GetBlobServicesDefaultEndpoint(),
        new GetTableServicesDefaultEndpoint(),
        new GetQueueServicesDefaultEndpoint(),
        new CreateOrUpdateArmTableEndpoint(logger),
        new GetArmTableEndpoint(logger),
        new DeleteArmTableEndpoint(logger),
        new CreateOrUpdateArmBlobContainerEndpoint(logger),
        new GetArmBlobContainerEndpoint(logger),
        new DeleteArmBlobContainerEndpoint(logger),
        new CreateOrUpdateArmQueueEndpoint(logger),
        new GetArmQueueEndpoint(logger),
        new RevokeUserDelegationKeysEndpoint(logger)
    ];
    
    public void Register()
    {
        var controlPlane = AzureStorageControlPlane.New(logger);

        eventPipeline.RegisterHandler<ContainerRegistryCreatedEventData>(
            ContainerRegistryCreatedEvent.EventName,
            data => controlPlane.CreateOrUpdate(SubscriptionIdentifier.From(data!.SubscriptionIdentifier!.Value),
                ResourceGroupIdentifier.From(data.ResourceGroupIdentifier), data.RegistryName!, CreateOrUpdateStorageAccountRequest.ForContainerRegistry(data.Location)));
    }
}
