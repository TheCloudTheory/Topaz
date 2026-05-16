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

namespace Topaz.Service.Storage.Endpoints.Queue;

internal abstract class QueueDataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly StorageResourceProvider _storageResourceProvider = new(logger);
    private readonly QueueStorageSecurityProvider _securityProvider = new(eventPipeline, logger);
    protected readonly ITopazLogger Logger = logger;

    /// <summary>
    /// Queue storage data-plane endpoints manage their own auth via <see cref="IsRequestAuthorized"/>.
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
            response.StatusCode = HttpStatusCode.Unauthorized;
            response.Headers.TryAddWithoutValidation("WWW-Authenticate", StorageDataPlaneAuthorizationChecker.WwwAuthenticateChallenge);
        }

        return authorized;
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
        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "Trying to get storage account.");

        if (!headers.TryGetValue("Host", out var host))
        {
            Logger.LogError("`Host` header not found - it's required for storage account creation.");

            storageAccount = null;
            return false;
        }

        var pathParts = host.ToString().Split('.');
        var accountName = pathParts[0];

        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(TryGetStorageAccount),
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
        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(TryGetStorageAccount),
            "Storage account '{0}' doesn't exist.", accountName);

        return false;
    }

    protected string GetQueueName(string path)
    {
        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(GetQueueName),
            "Looking for queue name in {0}", path);

        var pathParts = path.TrimStart('/').Split('/');

        Logger.LogDebug(nameof(QueueDataPlaneEndpointBase), nameof(GetQueueName), "Returning: {0}", pathParts[0]);

        return pathParts[0];
    }

    protected static bool TryGetQueueNameFromPath(string queuePath, out string? queueName)
    {
        var matches = Regex.Match(queuePath, @"^/([^/?]+)", RegexOptions.Compiled);
        if (matches.Success && matches.Groups.Count > 1)
        {
            queueName = matches.Groups[1].Value;
            return true;
        }

        queueName = null;
        return false;
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
}
