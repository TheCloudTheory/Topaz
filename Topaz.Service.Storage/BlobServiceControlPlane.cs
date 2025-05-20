using System.Net;
using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceControlPlane(BlobResourceProvider provider, ILogger logger)
{
    private readonly BlobResourceProvider provider = provider;
    private readonly ILogger logger = logger;

    public HttpStatusCode CreateContainer(string containerName, string storageAccountName)
    {
        this.provider.Create(containerName, storageAccountName);
        return HttpStatusCode.Created;
    }

    public ContainerEnumerationResult ListContainers(string storageAccountName)
    {
        var rawContainers = this.provider.List(storageAccountName);
        var containers = new List<Container>();

        foreach (var rawContainer in rawContainers)
        {
            var rawMetadata = File.ReadAllText(Path.Combine(rawContainer, "metadata.json"));
            containers.Add(JsonSerializer.Deserialize<Container>(rawMetadata, GlobalSettings.JsonOptions)!);
        }

        return new ContainerEnumerationResult(storageAccountName, containers.ToArray());
    }
}