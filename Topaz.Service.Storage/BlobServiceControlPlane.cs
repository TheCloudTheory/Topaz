using System.Net;
using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceControlPlane(BlobResourceProvider provider)
{
    public HttpStatusCode CreateContainer(string containerName, string storageAccountName)
    {
        provider.Create(storageAccountName, containerName);
        return HttpStatusCode.Created;
    }

    public ContainerEnumerationResult ListContainers(string storageAccountName)
    {
        var rawContainers = provider.List(storageAccountName);
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
    
    public string GetContainerBlobMetadataPath(string storageAccountName, string containerName)
    {
        return provider.GetContainerMetadataPath(storageAccountName, containerName);
    }
}