using System.Text.Json;
using Azure;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Serialization;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class BlobServiceDataPlane(BlobServiceControlPlane controlPlane, ITopazLogger logger)
{
    public DataPlaneOperationResult<BlobEnumerationResult> ListBlobs(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(ListBlobs), "Executing {0}: {1} {2}", nameof(ListBlobs), storageAccountName, containerName);
        
        var path = controlPlane.GetContainerDataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);
        var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
        var entities = files.Select(file => new Blob
        {
            Name = Path.GetRelativePath(path, file).Replace(Path.DirectorySeparatorChar, '/'),
            Properties = GetDeserializedBlobProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, containerName, Path.GetRelativePath(path, file).Replace(Path.DirectorySeparatorChar, '/'))
        }).ToArray();

        return new DataPlaneOperationResult<BlobEnumerationResult>(OperationResult.Success, new BlobEnumerationResult(storageAccountName, entities), null, null);
    }

    private BlobProperties? GetDeserializedBlobProperties(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName,
        string blobName)
    {
        var filePath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            $"/{containerName}/{blobName}");
        var content = File.ReadAllText(filePath);

        return JsonSerializer.Deserialize<BlobProperties>(content);
    }
    
    public DataPlaneOperationResult<BlobProperties> PutBlob(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, string blobName,
        Stream input, string? contentType = null, string? blobType = null, long? pageBlobSize = null)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(PutBlob), "Executing {0}: {1} {2} {3}", nameof(PutBlob), storageAccountName, blobPath, blobName);

        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        var blobDirectory = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(blobDirectory))
        {
            logger.LogError("Couldn't determine the blob directory.");
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "Couldn't determine the blob directory.", "InvalidBlobPath");
        }

        if (!Directory.Exists(blobDirectory))
        {
            logger.LogDebug(nameof(BlobServiceDataPlane), nameof(PutBlob), "Creating {0} for blob {1}...", blobDirectory, blobName);
            Directory.CreateDirectory(blobDirectory);
            logger.LogDebug(nameof(BlobServiceDataPlane), nameof(PutBlob), "Blob directory {0} created.", blobDirectory);
        }

        var resolvedBlobType = string.IsNullOrWhiteSpace(blobType) ? "BlockBlob" : blobType;

        if (resolvedBlobType == "PageBlob")
        {
            if (pageBlobSize == null || pageBlobSize <= 0)
            {
                logger.LogError("PageBlob requires a positive x-ms-blob-content-length.");
                return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "PageBlob requires x-ms-blob-content-length.", "InvalidBlobType");
            }

            using var pageContent = new MemoryStream();
            input.CopyTo(pageContent);

            if (pageContent.Length > 0 && pageContent.Length != pageBlobSize.Value)
            {
                logger.LogError(nameof(BlobServiceDataPlane), nameof(PutBlob),
                    "PageBlob payload length {0} must match x-ms-blob-content-length {1}.", pageContent.Length, pageBlobSize.Value);
                return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "PageBlob payload length must match x-ms-blob-content-length.", "InvalidBlobContentLength");
            }

            if (pageContent.Length == 0)
            {
                File.WriteAllBytes(fullPath, new byte[pageBlobSize.Value]);
            }
            else
            {
                File.WriteAllBytes(fullPath, pageContent.ToArray());
            }

            var metadata = new BlobProperties(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            {
                Name = blobName,
                BlobType = "PageBlob",
                ETag = new ETag(DateTimeOffset.UtcNow.Ticks.ToString()),
                ContentLength = pageBlobSize.Value,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                PageRanges = pageContent.Length == 0 ? [] : [new BlobPageRange(0, pageBlobSize.Value - 1)],
            };

            File.WriteAllText(GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath), JsonSerializer.Serialize(metadata));
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.Created, metadata, null, null);
        }
        else
        {
            using var sr = new StreamReader(input);
            var rawContent = sr.ReadToEnd();

            var metadata = new BlobProperties(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            {
                Name = blobName,
                ETag = new ETag(DateTimeOffset.Now.Ticks.ToString()),
                ContentLength = System.Text.Encoding.UTF8.GetByteCount(rawContent),
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            };

            File.WriteAllText(GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath), JsonSerializer.Serialize(metadata));
            File.WriteAllText(fullPath, rawContent);

            return new DataPlaneOperationResult<BlobProperties>(OperationResult.Created, metadata, null, null);
        }
    }

    public DataPlaneOperationResult<BlobProperties> PutPage(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        long startByte,
        long endByte,
        string pageWrite,
        Stream input)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(PutPage),
            "Executing {0}: {1} {2} range={3}-{4} write={5}", nameof(PutPage), storageAccountName, blobPath, startByte, endByte, pageWrite);

        if (startByte % 512 != 0 || (endByte + 1) % 512 != 0)
        {
            logger.LogError(nameof(BlobServiceDataPlane), nameof(PutPage),
                "Range bytes={0}-{1} is not 512-byte aligned.", startByte, endByte);
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "Page range must be 512-byte aligned.", "InvalidPageRange");
        }

        var propertiesPath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        if (!File.Exists(propertiesPath))
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.NotFound, null, null, null);

        var properties = JsonSerializer.Deserialize<BlobProperties>(File.ReadAllText(propertiesPath))!;

        if (properties.BlobType != "PageBlob")
        {
            logger.LogError(nameof(BlobServiceDataPlane), nameof(PutPage),
                "Blob {0} is not a PageBlob.", blobPath);
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "Put Page is only supported on page blobs.", "InvalidBlobType");
        }

        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        var rangeLength = endByte - startByte + 1;

        if (startByte < 0 || endByte < startByte || endByte >= properties.ContentLength)
        {
            logger.LogError(nameof(BlobServiceDataPlane), nameof(PutPage),
                "Range bytes={0}-{1} is outside blob length {2}.", startByte, endByte, properties.ContentLength);
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "Page range must be within the blob length.", "InvalidRange");
        }

        using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Write, FileShare.None))
        {
            fs.Seek(startByte, SeekOrigin.Begin);

            if (pageWrite.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                fs.Write(new byte[rangeLength], 0, (int)rangeLength);
                properties.PageRanges = ClearPageRange(properties.PageRanges, startByte, endByte);
            }
            else
            {
                using var ms = new MemoryStream();
                input.CopyTo(ms);
                if (ms.Length != rangeLength)
                {
                    logger.LogError(nameof(BlobServiceDataPlane), nameof(PutPage),
                        "Payload length {0} does not match requested page range length {1}.", ms.Length, rangeLength);
                    return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "Payload length must match the page range length.", "InvalidPageRange");
                }

                ms.Position = 0;
                ms.CopyTo(fs);
                properties.PageRanges = MergePageRange(properties.PageRanges, startByte, endByte);
            }
        }

        properties.ETag = new ETag(DateTimeOffset.UtcNow.Ticks.ToString());
        properties.LastModified = DateTimeOffset.UtcNow.ToString("R");

        File.WriteAllText(propertiesPath, JsonSerializer.Serialize(properties));

        return new DataPlaneOperationResult<BlobProperties>(OperationResult.Created, properties, null, null);
    }

    public DataPlaneOperationResult<BlobPageRangesResult> GetPageRanges(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        long? startByte = null,
        long? endByte = null)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(GetPageRanges),
            "Executing {0}: {1} {2} range={3}-{4}", nameof(GetPageRanges), storageAccountName, blobPath, startByte, endByte);

        var propertiesPath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        if (!File.Exists(propertiesPath))
            return new DataPlaneOperationResult<BlobPageRangesResult>(OperationResult.NotFound, null, null, null);

        var properties = JsonSerializer.Deserialize<BlobProperties>(File.ReadAllText(propertiesPath))!;

        if (properties.BlobType != "PageBlob")
        {
            logger.LogError(nameof(BlobServiceDataPlane), nameof(GetPageRanges),
                "Blob {0} is not a PageBlob.", blobPath);
            return new DataPlaneOperationResult<BlobPageRangesResult>(OperationResult.BadRequest, null, "Get Page Ranges is only supported on page blobs.", "InvalidBlobType");
        }

        if ((startByte.HasValue && !endByte.HasValue) || (!startByte.HasValue && endByte.HasValue))
        {
            return new DataPlaneOperationResult<BlobPageRangesResult>(OperationResult.BadRequest, null, "Both start and end range values are required.", "InvalidRange");
        }

        if (startByte.HasValue && endByte.HasValue &&
            (startByte.Value < 0 || endByte.Value < startByte.Value || endByte.Value >= properties.ContentLength))
        {
            return new DataPlaneOperationResult<BlobPageRangesResult>(OperationResult.BadRequest, null, "Requested range must be within the blob length.", "InvalidRange");
        }

        var filteredRanges = properties.PageRanges;
        if (startByte.HasValue && endByte.HasValue)
        {
            filteredRanges = properties.PageRanges
                .Select(range => IntersectRange(range, startByte.Value, endByte.Value))
                .Where(range => range != null)
                .Select(range => range!)
                .ToList();
        }

        return new DataPlaneOperationResult<BlobPageRangesResult>(
            OperationResult.Success,
            new BlobPageRangesResult
            {
                BlobProperties = properties,
                PageRanges = filteredRanges,
            },
            null,
            null);
    }

    public DataPlaneOperationResult<byte[]> GetBlobBytes(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(GetBlobBytes), "Executing {0}: {1} {2}", nameof(GetBlobBytes), storageAccountName, blobPath);

        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(fullPath))
            return new DataPlaneOperationResult<byte[]>(OperationResult.NotFound, null, null, null);

        return new DataPlaneOperationResult<byte[]>(OperationResult.Success, File.ReadAllBytes(fullPath), null, null);
    }

    private static List<BlobPageRange> MergePageRange(IEnumerable<BlobPageRange> existingRanges, long startByte, long endByte)
    {
        var orderedRanges = existingRanges
            .Append(new BlobPageRange(startByte, endByte))
            .OrderBy(range => range.Start)
            .ToList();

        if (orderedRanges.Count == 0)
            return [];

        var merged = new List<BlobPageRange> { orderedRanges[0] };
        foreach (var range in orderedRanges.Skip(1))
        {
            var current = merged[^1];
            if (range.Start <= current.End + 1)
            {
                merged[^1] = new BlobPageRange(current.Start, Math.Max(current.End, range.End));
                continue;
            }

            merged.Add(range);
        }

        return merged;
    }

    private static List<BlobPageRange> ClearPageRange(IEnumerable<BlobPageRange> existingRanges, long startByte, long endByte)
    {
        var remainingRanges = new List<BlobPageRange>();
        foreach (var range in existingRanges)
        {
            if (endByte < range.Start || startByte > range.End)
            {
                remainingRanges.Add(range);
                continue;
            }

            if (startByte > range.Start)
            {
                remainingRanges.Add(new BlobPageRange(range.Start, startByte - 1));
            }

            if (endByte < range.End)
            {
                remainingRanges.Add(new BlobPageRange(endByte + 1, range.End));
            }
        }

        return remainingRanges;
    }

    private static BlobPageRange? IntersectRange(BlobPageRange range, long startByte, long endByte)
    {
        var intersectionStart = Math.Max(range.Start, startByte);
        var intersectionEnd = Math.Min(range.End, endByte);

        return intersectionStart <= intersectionEnd
            ? new BlobPageRange(intersectionStart, intersectionEnd)
            : null;
    }

    public DataPlaneOperationResult PutBlock(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        string blockId,
        Stream input)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(PutBlock),
            "Executing {0}: {1} {2} blockId={3}", nameof(PutBlock), storageAccountName, blobPath, blockId);

        using var sr = new StreamReader(input);
        var rawContent = sr.ReadToEnd();

        var containerName = GetContainerNameFromBlobPath(blobPath);
        var blobSubpathKey = GetBlobSubpathKey(blobPath);
        var safeBlockId = blockId.Replace("/", "_").Replace("+", "-");

        var stagingDir = controlPlane.GetBlobBlocksStagingPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, containerName, blobSubpathKey);

        if (!Directory.Exists(stagingDir))
            Directory.CreateDirectory(stagingDir);

        File.WriteAllText(Path.Combine(stagingDir, safeBlockId), rawContent);
        // Persist original block ID for GetBlockList (safeBlockId may differ due to +/→-/_ substitutions)
        File.WriteAllText(Path.Combine(stagingDir, safeBlockId + ".meta"), blockId);

        return new DataPlaneOperationResult(OperationResult.Created, null, null);
    }

    public DataPlaneOperationResult<BlobProperties> PutBlockList(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        string blobName,
        Stream input,
        string? contentType = null)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(PutBlockList),
            "Executing {0}: {1} {2}", nameof(PutBlockList), storageAccountName, blobPath);

        using var sr = new StreamReader(input);
        var xml = sr.ReadToEnd();

        var request = BlockListCommitRequest.Parse(xml);
        var blockIds = request?.AllBlockIds.ToList();
        if (blockIds == null)
        {
            logger.LogError("PutBlockList: could not parse BlockList XML.");
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "Could not parse BlockList XML.", "InvalidXml");
        }

        var containerName = GetContainerNameFromBlobPath(blobPath);
        var blobSubpathKey = GetBlobSubpathKey(blobPath);
        var stagingDir = controlPlane.GetBlobBlocksStagingPath(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, containerName, blobSubpathKey);

        var parts = new List<string>(blockIds.Count);
        foreach (var blockId in blockIds)
        {
            var safeBlockId = blockId.Replace("/", "_").Replace("+", "-");
            var blockFile = Path.Combine(stagingDir, safeBlockId);
            if (!File.Exists(blockFile))
            {
                logger.LogError("PutBlockList: staged block '{0}' not found at '{1}'.", blockId, blockFile);
                return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, $"Staged block '{blockId}' not found.", "InvalidBlockList");
            }

            parts.Add(File.ReadAllText(blockFile));
        }

        var assembled = string.Concat(parts);
        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        var blobDirectory = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(blobDirectory))
        {
            logger.LogError("PutBlockList: couldn't determine the blob directory.");
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.BadRequest, null, "Couldn't determine the blob directory.", "InvalidBlobPath");
        }

        if (!Directory.Exists(blobDirectory))
            Directory.CreateDirectory(blobDirectory);

        var metadata = new BlobProperties(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        {
            Name = blobName,
            ETag = new ETag(DateTimeOffset.UtcNow.Ticks.ToString()),
            ContentLength = System.Text.Encoding.UTF8.GetByteCount(assembled),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
        };

        File.WriteAllText(GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath),
            JsonSerializer.Serialize(metadata));
        File.WriteAllText(fullPath, assembled);

        // Persist committed block list for GetBlockList before deleting the staging dir.
        var committedBlocks = blockIds
            .Zip(parts, (id, content) => new BlockRecord(id, System.Text.Encoding.UTF8.GetByteCount(content)))
            .ToList();
        File.WriteAllText(
            GetCommittedBlocksPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath),
            JsonSerializer.Serialize(committedBlocks));

        // Clean up staged blocks now that they have been committed.
        if (Directory.Exists(stagingDir))
            Directory.Delete(stagingDir, recursive: true);

        return new DataPlaneOperationResult<BlobProperties>(OperationResult.Created, metadata, null, null);
    }


    private static string GetBlobSubpathKey(string blobPath)
    {
        var segments = blobPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return string.Join("_", segments.Skip(1));
    }

    public DataPlaneOperationResult<BlobProperties> GetBlobProperties(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string blobPath, string blobName)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(GetBlobProperties), "Executing {0}: {1} {2} {3}", nameof(GetBlobProperties), storageAccountName, blobPath, blobName);

        var fullPath =
            GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(fullPath))
        {
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.NotFound, null, null, null);
        }

        var content = File.ReadAllText(fullPath);
        var properties = JsonSerializer.Deserialize<BlobProperties>(content);

        return new DataPlaneOperationResult<BlobProperties>(OperationResult.Success, properties, null, null);
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
        
        var containerName = segments[1] == ".blob" ?  segments[2] : segments[1];
        return containerName;
    }

    private string GetBlobPropertiesPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var metadataFileName = blobPath.Replace("/.blob", string.Empty).Replace("/", "_");
        var path = Path.Combine(controlPlane.GetContainerBlobMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName),
            $"{metadataFileName}.properties.json");

        return path;
    }

    private string GetCommittedBlocksPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var metadataFileName = blobPath.Replace("/.blob", string.Empty).Replace("/", "_");
        return Path.Combine(
            controlPlane.GetContainerBlobMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName),
            $"{metadataFileName}.committed-blocks.json");
    }

    public DataPlaneOperationResult<BlockListData> GetBlockList(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        string blockListType)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(GetBlockList),
            "Account: `{0}`, Path: {1}, Type: {2}", storageAccountName, blobPath, blockListType);

        var propertiesPath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        var blobExists = File.Exists(propertiesPath);

        var getCommitted = blockListType is "committed" or "all";
        var getUncommitted = blockListType is "uncommitted" or "all";

        // For committed-only requests, the blob must already exist
        if (getCommitted && !getUncommitted && !blobExists)
            return new DataPlaneOperationResult<BlockListData>(OperationResult.NotFound, null, null, null);

        // For uncommitted or all, determine staging dir existence for 404 check
        if (!blobExists && getUncommitted)
        {
            var containerName = GetContainerNameFromBlobPath(blobPath);
            var blobSubpathKey = GetBlobSubpathKey(blobPath);
            var stagingDir = controlPlane.GetBlobBlocksStagingPath(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, containerName, blobSubpathKey);
            if (!Directory.Exists(stagingDir))
                return new DataPlaneOperationResult<BlockListData>(OperationResult.NotFound, null, null, null);
        }

        List<BlockRecord> committed = [];
        List<BlockRecord> uncommitted = [];

        if (getCommitted)
        {
            var committedPath = GetCommittedBlocksPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
            if (File.Exists(committedPath))
                committed = JsonSerializer.Deserialize<List<BlockRecord>>(File.ReadAllText(committedPath)) ?? [];
        }

        if (getUncommitted)
        {
            var containerName = GetContainerNameFromBlobPath(blobPath);
            var blobSubpathKey = GetBlobSubpathKey(blobPath);
            var stagingDir = controlPlane.GetBlobBlocksStagingPath(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, containerName, blobSubpathKey);

            if (Directory.Exists(stagingDir))
            {
                foreach (var contentFile in Directory.EnumerateFiles(stagingDir)
                             .Where(f => !f.EndsWith(".meta"))
                             .OrderBy(f => f))
                {
                    var safeBlockId = Path.GetFileName(contentFile);
                    var metaPath = contentFile + ".meta";
                    var originalId = File.Exists(metaPath) ? File.ReadAllText(metaPath).Trim() : safeBlockId;
                    var size = (long)System.Text.Encoding.UTF8.GetByteCount(File.ReadAllText(contentFile));
                    uncommitted.Add(new BlockRecord(originalId, size));
                }
            }
        }

        return new DataPlaneOperationResult<BlockListData>(OperationResult.Success, new BlockListData(committed, uncommitted), null, null);
    }

    public DataPlaneOperationResult<string> GetBlob(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(GetBlob), "Executing {0}: {1} {2}", nameof(GetBlob),
            storageAccountName, blobPath);

        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(fullPath))
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null, null, null);

        return new DataPlaneOperationResult<string>(OperationResult.Success, File.ReadAllText(fullPath), null, null);
    }

    public DataPlaneOperationResult<CopyBlobData> CopyBlob(
        SubscriptionIdentifier srcSubscriptionId, ResourceGroupIdentifier srcResourceGroupId, string srcAccountName, string srcBlobPath,
        SubscriptionIdentifier dstSubscriptionId, ResourceGroupIdentifier dstResourceGroupId, string dstAccountName, string dstBlobPath, string dstBlobName)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(CopyBlob), "Copying from {0}/{1} to {2}/{3}",
            srcAccountName, srcBlobPath, dstAccountName, dstBlobPath);

        var srcContentPath = GetBlobPath(srcSubscriptionId, srcResourceGroupId, srcAccountName, srcBlobPath);
        var srcPropertiesPath = GetBlobPropertiesPath(srcSubscriptionId, srcResourceGroupId, srcAccountName, srcBlobPath);

        if (!File.Exists(srcContentPath))
            return new DataPlaneOperationResult<CopyBlobData>(OperationResult.NotFound, null, null, null);

        var dstContentPath = GetBlobPath(dstSubscriptionId, dstResourceGroupId, dstAccountName, dstBlobPath);
        var dstDirectory = Path.GetDirectoryName(dstContentPath);

        if (string.IsNullOrWhiteSpace(dstDirectory))
        {
            logger.LogError("Couldn't determine the destination blob directory.");
            return new DataPlaneOperationResult<CopyBlobData>(OperationResult.BadRequest, null, "Couldn't determine the destination blob directory.", "InvalidBlobPath");
        }

        if (!Directory.Exists(dstDirectory))
            Directory.CreateDirectory(dstDirectory);

        File.Copy(srcContentPath, dstContentPath, overwrite: true);

        var srcProperties = JsonSerializer.Deserialize<BlobProperties>(File.ReadAllText(srcPropertiesPath))!;
        var copyId = Guid.NewGuid().ToString();
        var dstProperties = new BlobProperties(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
        {
            Name = dstBlobName,
            ETag = new ETag(DateTimeOffset.UtcNow.Ticks.ToString()),
            ContentLength = srcProperties.ContentLength,
            ContentType = srcProperties.ContentType,
            ContentEncoding = srcProperties.ContentEncoding,
            ContentLanguage = srcProperties.ContentLanguage,
            CacheControl = srcProperties.CacheControl,
            ContentDisposition = srcProperties.ContentDisposition,
            CopyId = copyId,
        };

        var dstPropertiesPath = GetBlobPropertiesPath(dstSubscriptionId, dstResourceGroupId, dstAccountName, dstBlobPath);
        File.WriteAllText(dstPropertiesPath, JsonSerializer.Serialize(dstProperties));

        return new DataPlaneOperationResult<CopyBlobData>(OperationResult.Accepted, new CopyBlobData(dstProperties, copyId), null, null);
    }

    // TODO: Add support for `snapshot` and `versionid` query params
    public DataPlaneOperationResult DeleteBlob(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath, string blobName)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(DeleteBlob), "Executing {0}: {1} {2} {3}", nameof(DeleteBlob), storageAccountName, blobPath, blobName);
        
        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(fullPath))
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, null, null);
        }
        
        var fullPropertiesPathPath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);
        
        File.Delete(fullPath);
        File.Delete(fullPropertiesPathPath);
        
        return new DataPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public DataPlaneOperationResult<Dictionary<string, string>> GetBlobMetadata(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(GetBlobMetadata),
            "Account: `{0}`, Path: {1}", storageAccountName, blobPath);

        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(fullPath))
            return new DataPlaneOperationResult<Dictionary<string, string>>(OperationResult.NotFound, null, null, null);

        var metadataPath = GetBlobMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(metadataPath))
            return new DataPlaneOperationResult<Dictionary<string, string>>(OperationResult.Success, new Dictionary<string, string>(), null, null);

        var lines = File.ReadAllLines(metadataPath);
        var metadata = lines
            .Where(l => l.Contains('='))
            .ToDictionary(
                l => l[..l.IndexOf('=')],
                l => l[(l.IndexOf('=') + 1)..]);

        return new DataPlaneOperationResult<Dictionary<string, string>>(OperationResult.Success, metadata, null, null);
    }

    // TODO: Setting metadata should update / append values instead of replacing them
    public DataPlaneOperationResult SetBlobMetadata(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath,
        IHeaderDictionary headers)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(SetBlobMetadata),
            "Account: `{0}`, Path: {1}, Headers: {2}", storageAccountName, blobPath, headers);

        var fullPath = GetBlobPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(fullPath))
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, null, null);
        }

        var metadataHeaders = headers.Where(h => h.Key.StartsWith("x-ms-meta")).ToDictionary(h => h.Key, h => h.Value);
        var metadata = metadataHeaders.Select(h => $"{h.Key}={h.Value}").ToArray();

        File.WriteAllLines(
            GetBlobMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath),
            metadata);

        return new DataPlaneOperationResult(OperationResult.Updated, null, null);
    }

    public DataPlaneOperationResult<BlobProperties> SetBlobProperties(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        IHeaderDictionary headers)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(SetBlobProperties),
            "Account: `{0}`, Path: {1}", storageAccountName, blobPath);

        var propertiesPath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(propertiesPath))
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.NotFound, null, null, null);

        var properties = JsonSerializer.Deserialize<BlobProperties>(File.ReadAllText(propertiesPath))!;

        if (headers.TryGetValue("x-ms-blob-content-type", out var contentType) && !string.IsNullOrEmpty(contentType))
            properties.ContentType = contentType!;

        if (headers.TryGetValue("x-ms-blob-content-encoding", out var encoding) && !string.IsNullOrEmpty(encoding))
            properties.ContentEncoding = encoding!;

        if (headers.TryGetValue("x-ms-blob-content-language", out var language) && !string.IsNullOrEmpty(language))
            properties.ContentLanguage = language!;

        if (headers.TryGetValue("x-ms-blob-cache-control", out var cacheControl) && !string.IsNullOrEmpty(cacheControl))
            properties.CacheControl = cacheControl!;

        if (headers.TryGetValue("x-ms-blob-content-disposition", out var disposition) && !string.IsNullOrEmpty(disposition))
            properties.ContentDisposition = disposition!;

        properties.LastModified = DateTimeOffset.UtcNow.ToString("R");
        properties.ETag = new Azure.ETag(DateTimeOffset.UtcNow.Ticks.ToString());

        File.WriteAllText(propertiesPath, JsonSerializer.Serialize(properties));

        return new DataPlaneOperationResult<BlobProperties>(OperationResult.Updated, properties, null, null);
    }

    public DataPlaneOperationResult<BlobProperties> SetBlobProperties(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string blobPath,
        string? contentType,
        string? contentEncoding,
        string? contentLanguage,
        string? cacheControl,
        string? contentDisposition)
    {
        var propertiesPath = GetBlobPropertiesPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, blobPath);

        if (!File.Exists(propertiesPath))
            return new DataPlaneOperationResult<BlobProperties>(OperationResult.NotFound, null, null, null);

        var properties = JsonSerializer.Deserialize<BlobProperties>(File.ReadAllText(propertiesPath))!;

        if (contentType != null) properties.ContentType = contentType;
        if (contentEncoding != null) properties.ContentEncoding = contentEncoding;
        if (contentLanguage != null) properties.ContentLanguage = contentLanguage;
        if (cacheControl != null) properties.CacheControl = cacheControl;
        if (contentDisposition != null) properties.ContentDisposition = contentDisposition;

        properties.LastModified = DateTimeOffset.UtcNow.ToString("R");
        properties.ETag = new Azure.ETag(DateTimeOffset.UtcNow.Ticks.ToString());

        File.WriteAllText(propertiesPath, JsonSerializer.Serialize(properties));

        return new DataPlaneOperationResult<BlobProperties>(OperationResult.Updated, properties, null, null);
    }

    private string GetBlobMetadataPath(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string blobPath)
    {
        var containerName = GetContainerNameFromBlobPath(blobPath);
        var metadataFileName = blobPath.Replace("/", "_");
        var path = Path.Combine(controlPlane.GetContainerBlobMetadataPath(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName),
            $"{metadataFileName}.metadata.json");

        return path;
    }

    public DataPlaneOperationResult SetContainerMetadata(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName,
        IHeaderDictionary headers)
    {
        var metadataHeaders = headers
            .Where(h => h.Key.StartsWith("x-ms-meta-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        return SetContainerMetadata(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            containerName, metadataHeaders);
    }

    public DataPlaneOperationResult SetContainerMetadata(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string containerName,
        Dictionary<string, string> metadata)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(SetContainerMetadata),
            "Account: `{0}`, Container: {1}", storageAccountName, containerName);

        var (exists, metadataFilePath) = controlPlane.GetContainerMetadataState(subscriptionIdentifier,
            resourceGroupIdentifier, storageAccountName, containerName);

        if (!exists)
            return new DataPlaneOperationResult(OperationResult.NotFound, null, null);

        File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(metadata));

        return new DataPlaneOperationResult(OperationResult.Updated, null, null);
    }

    public DataPlaneOperationResult<Dictionary<string, string>> GetContainerMetadata(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string containerName)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(GetContainerMetadata),
            "Account: `{0}`, Container: {1}", storageAccountName, containerName);

        var (exists, metadataFilePath) = controlPlane.GetContainerMetadataState(subscriptionIdentifier,
            resourceGroupIdentifier, storageAccountName, containerName);

        if (!exists)
            return new DataPlaneOperationResult<Dictionary<string, string>>(OperationResult.NotFound, null, null, null);

        if (!File.Exists(metadataFilePath))
            return new DataPlaneOperationResult<Dictionary<string, string>>(OperationResult.Success, new Dictionary<string, string>(), null, null);

        var content = File.ReadAllText(metadataFilePath);
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(content) ?? new Dictionary<string, string>();

        return new DataPlaneOperationResult<Dictionary<string, string>>(OperationResult.Success, metadata, null, null);
    }

    public DataPlaneOperationResult<string> GetContainerAcl(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string containerName)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(GetContainerAcl),
            "Account: `{0}`, Container: {1}", storageAccountName, containerName);

        var (exists, aclFilePath) = controlPlane.GetContainerAclState(subscriptionIdentifier,
            resourceGroupIdentifier, storageAccountName, containerName);

        if (!exists)
            return new DataPlaneOperationResult<string>(OperationResult.NotFound, null, null, null);

        if (!File.Exists(aclFilePath))
            return new DataPlaneOperationResult<string>(OperationResult.Success, "<?xml version=\"1.0\" encoding=\"utf-8\"?><SignedIdentifiers />", null, null);

        var xml = File.ReadAllText(aclFilePath);
        return new DataPlaneOperationResult<string>(OperationResult.Success, xml, null, null);
    }

    public DataPlaneOperationResult SetContainerAcl(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string containerName,
        Stream input)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(SetContainerAcl),
            "Account: `{0}`, Container: {1}", storageAccountName, containerName);

        var (exists, aclFilePath) = controlPlane.GetContainerAclState(subscriptionIdentifier,
            resourceGroupIdentifier, storageAccountName, containerName);

        if (!exists)
            return new DataPlaneOperationResult(OperationResult.NotFound, null, null);

        using var sr = new StreamReader(input);
        var body = sr.ReadToEnd();

        // Normalise an empty body to an empty SignedIdentifiers document
        if (string.IsNullOrWhiteSpace(body))
            body = "<?xml version=\"1.0\" encoding=\"utf-8\"?><SignedIdentifiers />";

        File.WriteAllText(aclFilePath, body);
        return new DataPlaneOperationResult(OperationResult.Updated, null, null);
    }

    /// <summary>
    /// Implements the Lease Container operation.
    /// Returns the operation result plus the resulting lease state (null on error).
    /// </summary>
    public DataPlaneOperationResult<ContainerLease> LeaseContainer(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string containerName,
        string leaseAction,
        int leaseDuration,
        string? proposedLeaseId,
        string? currentLeaseId,
        int? breakPeriod)
    {
        logger.LogDebug(nameof(BlobServiceDataPlane), nameof(LeaseContainer),
            "Account: `{0}`, Container: {1}, Action: {2}", storageAccountName, containerName, leaseAction);

        var (exists, leaseFilePath) = controlPlane.GetContainerLeaseState(subscriptionIdentifier,
            resourceGroupIdentifier, storageAccountName, containerName);

        if (!exists)
            return new DataPlaneOperationResult<ContainerLease>(OperationResult.NotFound, null, null, null);

        // Load or initialize the current lease
        ContainerLease lease;
        if (File.Exists(leaseFilePath))
        {
            lease = JsonSerializer.Deserialize<ContainerLease>(File.ReadAllText(leaseFilePath),
                GlobalSettings.JsonOptions) ?? new ContainerLease();
        }
        else
        {
            lease = new ContainerLease();
        }

        var effectiveState = lease.EffectiveState();

        switch (leaseAction.ToLowerInvariant())
        {
            case "acquire":
            {
                if (effectiveState == ContainerLeaseState.Leased)
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.Conflict, null, null, null);

                var newLeaseId = string.IsNullOrEmpty(proposedLeaseId) ? Guid.NewGuid().ToString() : proposedLeaseId;
                lease.LeaseId = newLeaseId;
                lease.State = ContainerLeaseState.Leased;
                lease.Duration = leaseDuration;
                lease.ExpiresAt = leaseDuration == -1 ? null : DateTimeOffset.UtcNow.AddSeconds(leaseDuration);
                lease.BreakTime = null;
                SaveLease(leaseFilePath, lease);
                return new DataPlaneOperationResult<ContainerLease>(OperationResult.Created, lease, null, null);
            }
            case "renew":
            {
                if (effectiveState != ContainerLeaseState.Leased)
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.Conflict, null, null, null);
                if (!string.Equals(lease.LeaseId, currentLeaseId, StringComparison.OrdinalIgnoreCase))
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.PreconditionFailed, null, null, null);

                lease.ExpiresAt = lease.Duration == -1 ? null : DateTimeOffset.UtcNow.AddSeconds(lease.Duration);
                lease.State = ContainerLeaseState.Leased;
                lease.BreakTime = null;
                SaveLease(leaseFilePath, lease);
                return new DataPlaneOperationResult<ContainerLease>(OperationResult.Success, lease, null, null);
            }
            case "change":
            {
                if (effectiveState != ContainerLeaseState.Leased)
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.Conflict, null, null, null);
                if (!string.Equals(lease.LeaseId, currentLeaseId, StringComparison.OrdinalIgnoreCase))
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.PreconditionFailed, null, null, null);
                if (string.IsNullOrEmpty(proposedLeaseId))
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.BadRequest, null, null, null);

                lease.LeaseId = proposedLeaseId;
                SaveLease(leaseFilePath, lease);
                return new DataPlaneOperationResult<ContainerLease>(OperationResult.Success, lease, null, null);
            }
            case "release":
            {
                if (effectiveState != ContainerLeaseState.Leased)
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.Conflict, null, null, null);
                if (!string.Equals(lease.LeaseId, currentLeaseId, StringComparison.OrdinalIgnoreCase))
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.PreconditionFailed, null, null, null);

                lease.State = ContainerLeaseState.Available;
                lease.LeaseId = null;
                lease.ExpiresAt = null;
                lease.BreakTime = null;
                SaveLease(leaseFilePath, lease);
                return new DataPlaneOperationResult<ContainerLease>(OperationResult.Success, lease, null, null);
            }
            case "break":
            {
                if (effectiveState == ContainerLeaseState.Available || effectiveState == ContainerLeaseState.Broken)
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.Conflict, null, null, null);

                if (effectiveState == ContainerLeaseState.Breaking)
                {
                    // Already breaking — just return remaining time
                    return new DataPlaneOperationResult<ContainerLease>(OperationResult.Accepted, lease, null, null);
                }

                // Determine how long until the lease breaks
                int breakSeconds;
                if (breakPeriod.HasValue)
                {
                    breakSeconds = breakPeriod.Value;
                }
                else if (lease.Duration == -1)
                {
                    breakSeconds = 0;
                }
                else
                {
                    breakSeconds = lease.ExpiresAt.HasValue
                        ? (int)Math.Max(0, (lease.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds)
                        : 0;
                }

                lease.State = ContainerLeaseState.Breaking;
                lease.BreakTime = DateTimeOffset.UtcNow.AddSeconds(breakSeconds);
                SaveLease(leaseFilePath, lease);
                return new DataPlaneOperationResult<ContainerLease>(OperationResult.Accepted, lease, null, null);
            }
            default:
                return new DataPlaneOperationResult<ContainerLease>(OperationResult.BadRequest, null, null, null);
        }
    }

    private static void SaveLease(string path, ContainerLease lease)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(lease, GlobalSettings.JsonOptions));
    }
}
