using System.Net;
using System.Text.Json;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceControlPlane(BlobResourceProvider provider)
{
    public HttpStatusCode CreateContainer(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string containerName, string storageAccountName)
    {
        provider.Create(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        return HttpStatusCode.Created;
    }

    public ContainerEnumerationResult ListContainers(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName)
    {
        var rawContainers = provider.List(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        var containers = new List<Container>();

        foreach (var rawContainer in rawContainers)
        {
            var rawMetadata = File.ReadAllText(Path.Combine(rawContainer, "metadata.json"));
            containers.Add(JsonSerializer.Deserialize<Container>(rawMetadata, GlobalSettings.JsonOptions)!);
        }

        return new ContainerEnumerationResult(storageAccountName, containers.ToArray());
    }

    public string GetContainerDataPath(string storageAccountName, string containerName)
    {
        return provider.GetContainerDataPath(storageAccountName, containerName);
    }
    
    public string GetContainerBlobMetadataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        return provider.GetContainerMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
    }
}