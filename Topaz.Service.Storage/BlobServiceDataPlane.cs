using System.Net;
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
        var entities = files.Select(file => {
            var fi = new FileInfo(file);
            return new Blob { Name = fi.Name };
        }).ToArray();

        return new BlobEnumerationResult(storageAccountName, entities); 
    }

    // TODO: This method must support different kinds of blobs
    public HttpStatusCode PutBlob(string storageAccountName, string blobPath, string blobName, Stream input)
    {
        logger.LogDebug($"Executing {nameof(PutBlob)}: {storageAccountName} {blobPath} {blobName}");

        var containerName = blobPath.Split('/')[0];
        var path = controlPlane.GetContainerDataPath(storageAccountName, containerName);
        
        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();
        var virtualPath = blobPath.Split('/').Skip(1).Aggregate(Path.Combine);
        var fullPath = Path.Combine(path, virtualPath);
        var blobDirectory = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(blobDirectory))
        {
            logger.LogError("Couldn't determine the blob directory.");
            return HttpStatusCode.BadRequest;
        }
        
        if (Directory.Exists(blobDirectory) == false)
        {
            logger.LogDebug($"Creating {blobDirectory} for blob {blobName}...");
            Directory.CreateDirectory(blobDirectory);
            logger.LogDebug($"Blob directory {blobDirectory} created.");
        }
        
        File.WriteAllText(fullPath, rawContent);
        return HttpStatusCode.Created;
    }
}