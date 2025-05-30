using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceDataPlane(BlobServiceControlPlane controlPlane, ILogger logger)
{
    public BlobEnumerationResult ListBlobs(string storageAccountName, string containerName)
    {
        logger.LogDebug($"Executing {nameof(ListBlobs)}: {storageAccountName} {containerName}");
        
        var path = controlPlane.GetContainerDataPath(storageAccountName, containerName);
        var files = Directory.EnumerateFiles(path);
        var entities = files.Select(e => {
            var content = File.ReadAllText(e);
            return JsonSerializer.Deserialize<Blob>(content, GlobalSettings.JsonOptions)!;
        }).ToArray();

        return new BlobEnumerationResult(storageAccountName, entities); 
    }
}