using System.Net;
using System.Text.Json;
using Azure;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceDataPlane(BlobServiceControlPlane controlPlane, ITopazLogger logger)
{
    public BlobEnumerationResult ListBlobs(string storageAccountName, string containerName)
    {
        logger.LogDebug($"Executing {nameof(ListBlobs)}: {storageAccountName} {containerName}");
        
        var path = controlPlane.GetContainerDataPath(storageAccountName, containerName);
        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
        var entities = files.Select(file => new Blob { Name = file, Properties = GetDeserializedBlobProperties(storageAccountName, file)}).ToArray();

        return new BlobEnumerationResult(storageAccountName, entities); 
    }

    private BlobProperties? GetDeserializedBlobProperties(string storageAccountName, string localBlobPath)
    {
        var prefix = $".topaz/{AzureStorageService.LocalDirectoryPath}/{storageAccountName}/{BlobStorageService.LocalDirectoryPath}";
        var filePath = GetBlobPropertiesPath(storageAccountName, localBlobPath.Replace(prefix, string.Empty));
        var content = File.ReadAllText(filePath);
        
        return JsonSerializer.Deserialize<BlobProperties>(content);
    }

    // TODO: This method must support different kinds of blobs
    public (HttpStatusCode code, BlobProperties? properties) PutBlob(string storageAccountName, string blobPath, string blobName, Stream input)
    {
        logger.LogDebug($"Executing {nameof(PutBlob)}: {storageAccountName} {blobPath} {blobName}");
        
        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();
        var fullPath = GetBlobPath(storageAccountName, blobPath);
        var blobDirectory = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(blobDirectory))
        {
            logger.LogError("Couldn't determine the blob directory.");
            return (HttpStatusCode.BadRequest, null);
        }
        
        if (Directory.Exists(blobDirectory) == false)
        {
            logger.LogDebug($"Creating {blobDirectory} for blob {blobName}...");
            Directory.CreateDirectory(blobDirectory);
            logger.LogDebug($"Blob directory {blobDirectory} created.");
        }

        var metadata = new BlobProperties
        {
            Name = blobName,
            DateUploaded = DateTimeOffset.UtcNow,
            ETag = new ETag(DateTimeOffset.Now.Ticks.ToString()),
            LastModified = DateTimeOffset.UtcNow
        };
        
        File.WriteAllText(GetBlobPropertiesPath(storageAccountName, blobPath), JsonSerializer.Serialize(metadata));
        File.WriteAllText(fullPath, rawContent);
        
        return (HttpStatusCode.Created, metadata);
    }

    public (HttpStatusCode code, BlobProperties? properties) GetBlobProperties(string storageAccountName, string blobPath, string blobName)
    {
        logger.LogDebug($"Executing {nameof(GetBlobProperties)}: {storageAccountName} {blobPath} {blobName}");
        
        var fullPath = GetBlobPropertiesPath(storageAccountName, blobPath);

        if (!File.Exists(fullPath))
        {
            return (HttpStatusCode.NotFound, null);
        }
        
        var content = File.ReadAllText(fullPath);
        var properties = JsonSerializer.Deserialize<BlobProperties>(content);
        
        return (HttpStatusCode.OK, properties);
    }

    /// <summary>
    /// Returns full physical path for a blob file. 
    /// </summary>
    /// <returns>Physical path for a blob</returns>
    private string GetBlobPath(string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var path = controlPlane.GetContainerDataPath(storageAccountName, containerName);
        var virtualPath = blobPath.Split('/').Aggregate(Path.Combine);
        var fullPath = Path.Combine(path, virtualPath);

        return fullPath;
    }

    private string GetContainerNameFromBlobPath(string blobPath)
    {
        var segments = blobPath.Split('/');
        if (segments.Length == 1 && !blobPath.StartsWith($"/"))
        {
            return blobPath;
        }
        
        var containerName = segments[1];
        return containerName;
    }

    private string GetBlobPropertiesPath(string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var metadataFileName = blobPath.Replace("/", "_");
        var path = Path.Combine(controlPlane.GetContainerBlobMetadataPath(storageAccountName, containerName),
            $"{metadataFileName}.properties.json");

        return path;
    }

    // TODO: Add support for `snapshot` and `versionid` query params
    public HttpStatusCode DeleteBlob(string storageAccountName, string blobPath, string blobName)
    {
        logger.LogDebug($"Executing {nameof(DeleteBlob)}: {storageAccountName} {blobPath} {blobName}");
        
        var fullPath = GetBlobPath(storageAccountName, blobPath);

        if (File.Exists(fullPath) == false)
        {
            return HttpStatusCode.NotFound;
        }
        
        var fullPropertiesPathPath = GetBlobPropertiesPath(storageAccountName, blobPath);
        
        File.Delete(fullPath);
        File.Delete(fullPropertiesPathPath);
        
        return HttpStatusCode.Accepted;
    }

    // TODO: Setting metadata should update / append values instead of replacing them
    public HttpStatusCode SetBlobMetadata(string storageAccountName, string blobPath, IHeaderDictionary headers)
    {
        logger.LogDebug($"Executing {nameof(SetBlobMetadata)}: {storageAccountName} {blobPath}");
        
        var fullPath = GetBlobPath(storageAccountName, blobPath);

        if (File.Exists(fullPath) == false)
        {
            return HttpStatusCode.NotFound;
        }
        
        var metadataHeaders = headers.Where(h => h.Key.StartsWith("x-ms-meta")).ToDictionary(h => h.Key, h => h.Value);
        var metadata = metadataHeaders.Select(h => $"{h.Key}={h.Value}").ToArray();
        
        File.WriteAllLines(GetBlobMetadataPath(storageAccountName, blobPath), metadata);
        
        return HttpStatusCode.OK;
    }
    
    private string GetBlobMetadataPath(string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var metadataFileName = blobPath.Replace("/", "_");
        var path = Path.Combine(controlPlane.GetContainerBlobMetadataPath(storageAccountName, containerName),
            $"{metadataFileName}.metadata.json");

        return path;
    }
}