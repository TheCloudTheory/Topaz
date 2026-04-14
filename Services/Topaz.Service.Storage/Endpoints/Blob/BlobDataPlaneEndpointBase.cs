using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Services;
using Topaz.Shared;
using BlobProperties = Topaz.Service.Storage.Models.BlobProperties;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal abstract class BlobDataPlaneEndpointBase(ITopazLogger logger)
{
    private readonly ResourceProvider _resourceProvider = new(logger);
    protected readonly ITopazLogger Logger = logger;

    protected bool TryGetStorageAccount(IHeaderDictionary headers, out StorageAccountResource? storageAccount)
    {
        Logger.LogDebug(nameof(BlobDataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "Trying to get storage account.");

        if (!headers.TryGetValue("Host", out var host))
        {
            Logger.LogError("`Host` header not found - it's required for storage account creation.");

            storageAccount = null;
            return false;
        }

        var pathParts = host.ToString().Split('.');
        var accountName = pathParts[0];

        Logger.LogDebug(nameof(BlobDataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "About to check if storage account '{0}' exists.", accountName);

        var identifiers = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, accountName!);
        if (identifiers != null)
        {
            storageAccount = _resourceProvider.GetAs<StorageAccountResource>(
                SubscriptionIdentifier.From(identifiers.Value.subscription),
                ResourceGroupIdentifier.From(identifiers.Value.resourceGroup), accountName);
            return true;
        }

        storageAccount = null;
        Logger.LogDebug(nameof(BlobDataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "Storage account '{0}' doesn't exists.", accountName);

        return false;
    }

    protected string GetContainerName(string path)
    {
        Logger.LogDebug(nameof(BlobDataPlaneEndpointBase), nameof(GetContainerName),
            "Looking for container name in {0}", path);

        var pathParts = path.Split('/');

        Logger.LogDebug(nameof(BlobDataPlaneEndpointBase), nameof(GetContainerName), "Returning: {0}", pathParts[1]);

        return pathParts[1];
    }

    protected static bool TryGetBlobName(string blobPath, out string? blobName)
    {
        var matches = Regex.Match(blobPath, @"[^/]+$", RegexOptions.Compiled);
        if (matches.Success)
        {
            blobName = matches.Groups[0].Value;
            return true;
        }

        blobName = null;
        return false;
    }

    protected static void SetResponseHeaders(HttpResponseMessage response, BlobProperties properties)
    {
        var etag = properties.ETag.ToString();
        if (!etag.StartsWith('"') && !etag.EndsWith('"'))
        {
            // Note we're enclosing ETag header explicitly with double quotes to align it
            // with RFC description stating this tag is "an entity tag consists of an opaque quoted string, possibly prefixed by a weakness indicator"
            response.Headers.Add("ETag", $"\"{etag}\"");
        }

        // Adding `Last-Modified` directly as response header fail with an error stating
        // we're misusing that header. However, based on the behavior of Blob Storage SDK
        // it looks like it expects that header to be part of the response headers, not response
        // content. For now, we can leave it as it is (as SDK fallbacks to ETag anyway),
        // but it may be worth considering adding that header without validation if possible.
        var emptyContent = new StringContent(string.Empty);
        emptyContent.Headers.LastModified = DateTimeOffset.Parse(properties.LastModified!, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal);
        response.Content = emptyContent;
    }
}
