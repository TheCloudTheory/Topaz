using System.Net;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Security;
using Topaz.Service.Storage.Serialization;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceControlPlane(BlobResourceProvider provider)
{
    public ControlPlaneOperationResult<Container> CreateContainer(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string containerName, string storageAccountName,
        IHeaderDictionary? headers = null)
    {
        if (provider.ContainerExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName))
            return new ControlPlaneOperationResult<Container>(OperationResult.Conflict, null, null, null);

        var container = new Container
        {
            Name = containerName
        };

        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName, container);

        var publicAccess = headers != null && headers.TryGetValue("x-ms-blob-public-access", out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString() : null;
        if (!string.IsNullOrWhiteSpace(publicAccess))
            provider.SetContainerPublicAccess(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, containerName, publicAccess);

        return new ControlPlaneOperationResult<Container>(OperationResult.Created, container, null, null);
    }

    public ControlPlaneOperationResult<ContainerEnumerationResult> ListContainers(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName)
    {
        var containers =
            provider.ListAs<Container>(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, 10);

        return new ControlPlaneOperationResult<ContainerEnumerationResult>(
            OperationResult.Success,
            new ContainerEnumerationResult(storageAccountName, containers.ToArray()!),
            null,
            null);
    }
    
    public ControlPlaneOperationResult DeleteContainer(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        provider.DeleteContainer(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        return new ControlPlaneOperationResult(OperationResult.Success);
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

    public static string GetBlobServiceStatsXml()
    {
        var lastSyncTime = DateTimeOffset.UtcNow.ToString("R");
        return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <StorageServiceStats>
                  <GeoReplication>
                    <Status>live</Status>
                    <LastSyncTime>{lastSyncTime}</LastSyncTime>
                  </GeoReplication>
                </StorageServiceStats>
                """;
    }

    public string? GetContainerPublicAccess(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        if (!provider.ContainerExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName))
            return null;
        return provider.GetContainerPublicAccess(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
    }

    public void SetContainerPublicAccess(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName,
        string? accessLevel)
    {
        provider.SetContainerPublicAccess(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName, accessLevel);
    }

    public StoredAccessPolicy? GetContainerStoredPolicy(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string containerName,
        string policyId)
    {
        var filePath = provider.GetContainerAclFilePath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, containerName);
        if (!File.Exists(filePath)) return null;

        var doc = XDocument.Load(filePath);
        var match = doc.Descendants("SignedIdentifier")
            .FirstOrDefault(e => (string?)e.Element("Id") == policyId);
        if (match == null) return null;

        return new StoredAccessPolicy(
            (string?)match.Element("Permission"),
            (string?)match.Element("Start"),
            (string?)match.Element("Expiry"));
    }
}
