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
    
    public System.Net.HttpStatusCode DeleteContainer(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        provider.DeleteContainer(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        return System.Net.HttpStatusCode.NoContent;
    }

    public (bool exists, string metadataFilePath) GetContainerMetadataState(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string containerName)
    {
        var exists = provider.ContainerExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        var filePath = provider.GetContainerMetadataFilePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        return (exists, filePath);
    }

    public (bool exists, string aclFilePath) GetContainerAclState(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string containerName)
    {
        var exists = provider.ContainerExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        var filePath = provider.GetContainerAclFilePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        return (exists, filePath);
    }

    public (bool exists, string leaseFilePath) GetContainerLeaseState(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string containerName)
    {
        var exists = provider.ContainerExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        var filePath = provider.GetContainerLeaseFilePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        return (exists, filePath);
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

    public string GetBlobBlocksStagingPath(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName,
        string blobSubpathKey)
    {
        return provider.GetBlobBlocksStagingPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            containerName, blobSubpathKey);
    }
}