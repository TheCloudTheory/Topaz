using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Topaz.Dns;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Security;
using Topaz.Service.Storage.Services;
using Topaz.Shared;
using BlobProperties = Topaz.Service.Storage.Models.BlobProperties;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal abstract class BlobDataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly StorageResourceProvider _storageResourceProvider = new(logger);
    private readonly BlobStorageSecurityProvider _securityProvider = new(eventPipeline, logger);
    protected readonly ITopazLogger Logger = logger;

    /// <summary>
    /// Blob storage data-plane endpoints manage their own auth via <see cref="IsRequestAuthorized"/>.
    /// The router's default ARM RBAC check is bypassed here.
    /// </summary>
    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker) => (true, null);

    protected bool IsRequestAuthorized(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string[] requiredPermissions,
        HttpContext context,
        HttpResponseMessage response)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out _))
        {
            response.StatusCode = System.Net.HttpStatusCode.Unauthorized;
            response.Headers.TryAddWithoutValidation("WWW-Authenticate", StorageDataPlaneAuthorizationChecker.WwwAuthenticateChallenge);
            return false;
        }
        var rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget
                        ?? context.Request.Path.Value
                        ?? string.Empty;
        var queryIndex = rawTarget.IndexOf('?');
        var rawPath = queryIndex >= 0 ? rawTarget[..queryIndex] : rawTarget;
        return _securityProvider.RequestIsAuthorized(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, context.Request.Headers, requiredPermissions, context.Request.Method,
            rawPath, context.Request.QueryString);
    }

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
            storageAccount = _storageResourceProvider.GetAs<StorageAccountResource>(
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
