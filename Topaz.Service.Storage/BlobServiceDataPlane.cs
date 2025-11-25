using System.Net;
using System.Text.Json;
using Azure;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Service.Storage.Services;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceDataPlane(BlobServiceControlPlane controlPlane, ITopazLogger logger)
{
    public BlobEnumerationResult ListBlobs(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        logger.LogDebug($"Executing {nameof(ListBlobs)}: {storageAccountName} {containerName}");
        
        var path = controlPlane.GetContainerDataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
        var entities = files.Select(file => new Blob
        {
            Name = file,
            Properties = GetDeserializedBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, file)
        }).ToArray();

        return new BlobEnumerationResult(storageAccountName, entities); 
    }

    private BlobProperties? GetDeserializedBlobProperties(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName,
        string localBlobPath)
    {
        // Note that we will perform a 2-step cleanup for the file path. The reason for that 
        // is that physical file path is a completely different concept than a virtual
        // path used on a service level
        var servicePath = controlPlane.GetServicePath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        var filePath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            localBlobPath.Replace(servicePath, string.Empty).Replace("data/", string.Empty));
        var content = File.ReadAllText(filePath);

        return JsonSerializer.Deserialize<BlobProperties>(content);
    }

    // TODO: This method must support different kinds of blobs
    public (HttpStatusCode code, BlobProperties? properties) PutBlob(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, string blobName,
        Stream input)
    {
        logger.LogDebug($"Executing {nameof(PutBlob)}: {storageAccountName} {blobPath} {blobName}");

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();
        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        var blobDirectory = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(blobDirectory))
        {
            logger.LogError("Couldn't determine the blob directory.");
            return (HttpStatusCode.BadRequest, null);
        }

        if (!Directory.Exists(blobDirectory))
        {
            logger.LogDebug($"Creating {blobDirectory} for blob {blobName}...");
            Directory.CreateDirectory(blobDirectory);
            logger.LogDebug($"Blob directory {blobDirectory} created.");
        }

        var metadata = new BlobProperties(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        {
            Name = blobName,
            ETag = new ETag(DateTimeOffset.Now.Ticks.ToString()),
        };

        File.WriteAllText(GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath), JsonSerializer.Serialize(metadata));
        File.WriteAllText(fullPath, rawContent);

        return (HttpStatusCode.Created, metadata);
    }

    public (HttpStatusCode code, BlobProperties? properties) GetBlobProperties(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string blobPath, string blobName)
    {
        logger.LogDebug($"Executing {nameof(GetBlobProperties)}: {storageAccountName} {blobPath} {blobName}");

        var fullPath =
            GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

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
    private string GetBlobPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var path = controlPlane.GetContainerDataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        
        // We will skip two initial elements being /<container-name> so a blob
        // path doesn't contain a duplicated value
        var segments = blobPath.Split('/');
        var virtualPath = segments.Length > 2 ? segments.Skip(2).Aggregate(Path.Combine) : segments.Aggregate(Path.Combine);
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

    private string GetBlobPropertiesPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var metadataFileName = blobPath.Replace("/", "_");
        var path = Path.Combine(controlPlane.GetContainerBlobMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName),
            $"{metadataFileName}.properties.json");

        return path;
    }

    // TODO: Add support for `snapshot` and `versionid` query params
    public HttpStatusCode DeleteBlob(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, string blobName)
    {
        logger.LogDebug($"Executing {nameof(DeleteBlob)}: {storageAccountName} {blobPath} {blobName}");
        
        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(fullPath))
        {
            return HttpStatusCode.NotFound;
        }
        
        var fullPropertiesPathPath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        
        File.Delete(fullPath);
        File.Delete(fullPropertiesPathPath);
        
        return HttpStatusCode.Accepted;
    }

    // TODO: Setting metadata should update / append values instead of replacing them
    public HttpStatusCode SetBlobMetadata(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, IHeaderDictionary headers)
    {
        logger.LogDebug($"Executing {nameof(SetBlobMetadata)}: {storageAccountName} {blobPath}");
        
        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(fullPath))
        {
            return HttpStatusCode.NotFound;
        }
        
        var metadataHeaders = headers.Where(h => h.Key.StartsWith("x-ms-meta")).ToDictionary(h => h.Key, h => h.Value);
        var metadata = metadataHeaders.Select(h => $"{h.Key}={h.Value}").ToArray();
        
        File.WriteAllLines(GetBlobMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath), metadata);
        
        return HttpStatusCode.OK;
    }
    
    private string GetBlobMetadataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var metadataFileName = blobPath.Replace("/", "_");
        var path = Path.Combine(controlPlane.GetContainerBlobMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName),
            $"{metadataFileName}.metadata.json");

        return path;
    }
}