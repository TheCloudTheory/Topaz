using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
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
        var rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget
                        ?? context.Request.Path.Value
                        ?? string.Empty;
        var queryIndex = rawTarget.IndexOf('?');
        var rawPath = queryIndex >= 0 ? rawTarget[..queryIndex] : rawTarget;

        var authorized = _securityProvider.RequestIsAuthorized(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, context.Request.Headers, requiredPermissions, context.Request.Method,
            rawPath, context.Request.QueryString);

        if (!authorized)
        {
            const string errorXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                    "<Error><Code>AuthenticationFailed</Code>" +
                                    "<Message>Server failed to authenticate the request. " +
                                    "Make sure the value of the Authorization header is formed correctly including the signature.</Message></Error>";
            response.StatusCode = System.Net.HttpStatusCode.Unauthorized;
            response.Headers.TryAddWithoutValidation("WWW-Authenticate", StorageDataPlaneAuthorizationChecker.WwwAuthenticateChallenge);
            response.Content = new StringContent(errorXml, Encoding.UTF8, "application/xml");
        }

        return authorized;
    }

    protected bool TryGetStorageAccountFromSecondaryHost(IHeaderDictionary headers,
        out StorageAccountResource? storageAccount)
    {
        if (!headers.TryGetValue("Host", out var host))
        {
            storageAccount = null;
            return false;
        }

        var firstLabel = host.ToString().Split('.')[0];
        const string suffix = "-secondary";
        if (!firstLabel.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            storageAccount = null;
            return false;
        }

        var primaryName = firstLabel[..^suffix.Length];
        var identifiers = GlobalDnsEntries.GetEntry(AzureStorageService.UniqueName, primaryName);
        if (identifiers == null)
        {
            storageAccount = null;
            return false;
        }

        storageAccount = _storageResourceProvider.GetAs<StorageAccountResource>(
            SubscriptionIdentifier.From(identifiers.Value.subscription),
            ResourceGroupIdentifier.From(identifiers.Value.resourceGroup), primaryName);
        return storageAccount != null;
    }

    protected static bool RejectIfSecondaryHostForMutation(IHeaderDictionary headers, HttpResponseMessage response)
    {
        if (!headers.TryGetValue("Host", out var host)) return false;
        var firstLabel = host.ToString().Split('.')[0];
        if (!firstLabel.EndsWith("-secondary", StringComparison.OrdinalIgnoreCase)) return false;

        const string errorXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                 "<Error><Code>WriteOperationNotSupportedOnSecondary</Code>" +
                                 "<Message>The account being accessed does not support writes from the secondary region.</Message></Error>";
        response.StatusCode = HttpStatusCode.Forbidden;
        response.Content = new StringContent(errorXml, Encoding.UTF8, "application/xml");
        return true;
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
        var containerName = pathParts[1];

        // Inline guard: CodeQL's cs/path-injection PathCheck requires a direct boolean guard
        // on the tainted variable at the use site. Path.GetFileName (inside PathGuard.SanitizeName)
        // is only recognised as a sanitizer for cs/zipslip, not for cs/path-injection.
        if (containerName.Contains('/') || containerName.Contains('\\') || containerName.Contains(".."))
            throw new ArgumentException("Container name contains invalid characters.");

        Logger.LogDebug(nameof(BlobDataPlaneEndpointBase), nameof(GetContainerName), "Returning: {0}", containerName);

        return containerName;
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
