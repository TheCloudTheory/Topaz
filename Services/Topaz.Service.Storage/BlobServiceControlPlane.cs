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

        var now = DateTimeOffset.UtcNow;
        var container = new Container
        {
            Name = containerName,
            LastModified = now,
            Etag = $"\"{now.Ticks}\""
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

    public ControlPlaneOperationResult<string> GetBlobServicePropertiesXml(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName)
    {
        var path = provider.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        var propertiesFilePath = Path.Combine(path, "blob-service-properties.xml");

        if (!File.Exists(propertiesFilePath))
            return new ControlPlaneOperationResult<string>(OperationResult.Success, DefaultBlobServicePropertiesXml,
                null, null);

        return new ControlPlaneOperationResult<string>(OperationResult.Success,
            File.ReadAllText(propertiesFilePath), null, null);
    }

    public ControlPlaneOperationResult SetBlobServiceProperties(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, Stream input)
    {
        var path = provider.GetServiceInstancePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        var propertiesFilePath = Path.Combine(path, "blob-service-properties.xml");

        var document = XDocument.Load(input, LoadOptions.PreserveWhitespace);

        if (document.Root?.Element("Cors") == null)
            document.Root?.Add(new XElement("Cors"));

        document.Save(propertiesFilePath);
        return new ControlPlaneOperationResult(OperationResult.Success);
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

    private const string DefaultBlobServicePropertiesXml =
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
        "<StorageServiceProperties>" +
        "<Logging><Version>1.0</Version><Delete>false</Delete><Read>false</Read><Write>false</Write>" +
        "<RetentionPolicy><Enabled>false</Enabled></RetentionPolicy></Logging>" +
        "<HourMetrics><Version>1.0</Version><Enabled>false</Enabled>" +
        "<RetentionPolicy><Enabled>false</Enabled></RetentionPolicy></HourMetrics>" +
        "<MinuteMetrics><Version>1.0</Version><Enabled>false</Enabled>" +
        "<RetentionPolicy><Enabled>false</Enabled></RetentionPolicy></MinuteMetrics>" +
        "<Cors />" +
        "<DeleteRetentionPolicy><Enabled>false</Enabled></DeleteRetentionPolicy>" +
        "<StaticWebsite><Enabled>false</Enabled></StaticWebsite>" +
        "</StorageServiceProperties>";

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

        var accessPolicy = match.Element("AccessPolicy");
        return new StoredAccessPolicy(
            (string?)accessPolicy?.Element("Permission"),
            (string?)accessPolicy?.Element("Start"),
            (string?)accessPolicy?.Element("Expiry"));
    }
}
