using System.Net;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceControlPlane(BlobResourceProvider provider)
{
    public HttpStatusCode CreateContainer(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string containerName, string storageAccountName)
    {
        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName, new Container
        {
            Name = containerName
        });
        
        return HttpStatusCode.Created;
    }

    public ContainerEnumerationResult ListContainers(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var containers =
            provider.ListAs<Container>(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, 10);

        return new ContainerEnumerationResult(storageAccountName, containers.ToArray()!);
    }
    
    public string GetServicePath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        return provider.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
    }

    public string GetContainerDataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        return provider.GetContainerDataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
    }
    
    public string GetContainerBlobMetadataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        return provider.GetContainerMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
    }
}