using System.Net;
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
}