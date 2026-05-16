using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Security;

/// <summary>
/// Validates authorization for Azure Blob Storage data-plane requests.
/// Supports SharedKey (13-field Blob/Queue format), SharedKeyLite, Bearer (RBAC), and Service SAS.
/// </summary>
internal sealed class BlobStorageSecurityProvider(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly AzureStorageControlPlane _controlPlane = new(new StorageResourceProvider(logger), logger);
    private readonly StorageDataPlaneAuthorizationChecker _bearerChecker = new(eventPipeline, logger);
    private readonly BlobServiceControlPlane _blobControlPlane = new(new BlobResourceProvider(logger));
    private readonly ServiceSasValidator _sasValidator = new(new AzureStorageControlPlane(new StorageResourceProvider(logger), logger), logger);

    public bool RequestIsAuthorized(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        IHeaderDictionary headers,
        string[] requiredPermissions,
        string method,
        string absolutePath,
        QueryString query)
    {
        if (!headers.TryGetValue("Authorization", out var value) || string.IsNullOrEmpty(value))
        {
            if (ServiceSasValidator.IsServiceSas(query))
            {
                logger.LogDebug(nameof(BlobStorageSecurityProvider), nameof(RequestIsAuthorized),
                    "No Authorization header; attempting Service SAS validation for path='{0}'", absolutePath);
                return IsAuthorizedForServiceSas(
                    subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, absolutePath, query);
            }

            return IsAnonymousAccessAllowed(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, absolutePath, query, method);
        }

        var headerValue = value.ToString();
        var parts = headerValue.Split(' ', 2);
        var scheme = parts[0];

        logger.LogDebug(nameof(BlobStorageSecurityProvider), nameof(RequestIsAuthorized),
            "Scheme='{0}' Method='{1}' Path='{2}'", scheme, method, absolutePath);

        return scheme switch
        {
            "SharedKey" => IsAuthorizedForSharedKey(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, parts.Length > 1 ? parts[1] : string.Empty, headers, method, absolutePath, query),
            "SharedKeyLite" => IsAuthorizedForSharedKeyLite(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccountName, parts.Length > 1 ? parts[1] : string.Empty, headers, absolutePath, query),
            "Bearer" => _bearerChecker.IsAuthorizedForBearer(subscriptionIdentifier, requiredPermissions, headerValue),
            _ => LogAndDeny(scheme)
        };
    }

    private bool LogAndDeny(string scheme)
    {
        logger.LogError($"Authentication failure for Blob Storage. Unsupported scheme: {scheme}.");
        return false;
    }

    private bool IsAuthorizedForServiceSas(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string absolutePath,
        QueryString query)
    {
        // Derive the container name from the first path segment.
        var containerName = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        return _sasValidator.Validate(
            subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            absolutePath, query, ServiceSasValidator.SasServiceType.Blob,
            policyId => _blobControlPlane.GetContainerStoredPolicy(
                subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName, policyId));
    }

    private bool IsAuthorizedForSharedKey(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string headerValue,
        IHeaderDictionary headers,
        string method,
        string absolutePath,
        QueryString query)
    {
        var valueParts = headerValue.Split(':', 2);
        if (valueParts.Length != 2)
        {
            logger.LogError("Authentication failure for Blob SharedKey scheme. Header value format is incorrect.");
            return false;
        }

        var accountName = valueParts[0];
        var signature = valueParts[1];

        if (accountName != storageAccountName)
        {
            logger.LogError("Authentication failure for Blob SharedKey. Account name mismatch.");
            return false;
        }

        var storageAccountResult = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccountResult.Result != OperationResult.Success || storageAccountResult.Resource == null)
            throw new InvalidOperationException($"Storage account {storageAccountName} does not exist.");

        var stringToSign = BuildStringToSignForSharedKeyFull(storageAccountName, method, absolutePath, headers, query);

        logger.LogDebug(nameof(BlobStorageSecurityProvider), nameof(IsAuthorizedForSharedKey),
            "Path='{0}' StringToSign='{1}'", absolutePath, stringToSign.Replace("\n", "\\n"));

        var hash1 = ComputeHmacSha256(storageAccountResult.Resource.Keys[0].Value, stringToSign);
        var hash2 = ComputeHmacSha256(storageAccountResult.Resource.Keys[1].Value, stringToSign);

        if (hash1 == signature || hash2 == signature) return true;

        logger.LogError(nameof(BlobStorageSecurityProvider), nameof(IsAuthorizedForSharedKey),
            "Signature mismatch for account '{0}'. Expected one of [{1}, {2}], got {3}.",
            storageAccountName, hash1[..10], hash2[..10], signature[..10]);
        return false;
    }

    private bool IsAuthorizedForSharedKeyLite(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string headerValue,
        IHeaderDictionary headers,
        string absolutePath,
        QueryString query)
    {
        var valueParts = headerValue.Split(':', 2);
        if (valueParts.Length != 2)
        {
            logger.LogError("Authentication failure for Blob SharedKeyLite scheme. Header value format is incorrect.");
            return false;
        }

        var accountName = valueParts[0];
        var signature = valueParts[1];

        if (accountName != storageAccountName)
        {
            logger.LogError("Authentication failure for Blob SharedKeyLite. Account name mismatch.");
            return false;
        }

        var storageAccountResult = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccountResult.Result != OperationResult.Success || storageAccountResult.Resource == null)
            throw new InvalidOperationException($"Storage account {storageAccountName} does not exist.");

        headers.TryGetValue("x-ms-date", out var date);
        headers.TryGetValue("Date", out var fallbackDate);
        var dateStr = date.ToString() ?? fallbackDate.ToString();
        var canonicalizedResource = "/" + storageAccountName + absolutePath;
        if (query.TryGetValueForKey("comp", out var comp))
            canonicalizedResource += "?comp=" + comp;

        var stringToSign = dateStr + "\n" + canonicalizedResource;

        var hash1 = ComputeHmacSha256(storageAccountResult.Resource.Keys[0].Value, stringToSign);
        var hash2 = ComputeHmacSha256(storageAccountResult.Resource.Keys[1].Value, stringToSign);

        if (hash1 == signature || hash2 == signature) return true;

        logger.LogError(nameof(BlobStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLite),
            "Signature mismatch for account '{0}'.", storageAccountName);
        return false;
    }

    private static string BuildStringToSignForSharedKeyFull(
        string storageAccountName,
        string method,
        string absolutePath,
        IHeaderDictionary headers,
        QueryString query)
    {
        headers.TryGetValue("Content-Encoding", out var contentEncoding);
        headers.TryGetValue("Content-Language", out var contentLanguage);
        headers.TryGetValue("Content-Length", out var contentLength);
        headers.TryGetValue("Content-MD5", out var contentMd5);
        headers.TryGetValue("Content-Type", out var contentType);
        headers.TryGetValue("Date", out var date);
        headers.TryGetValue("If-Modified-Since", out var ifModifiedSince);
        headers.TryGetValue("If-Match", out var ifMatch);
        headers.TryGetValue("If-None-Match", out var ifNoneMatch);
        headers.TryGetValue("If-Unmodified-Since", out var ifUnmodifiedSince);
        headers.TryGetValue("Range", out var range);

        // When x-ms-date is present, Date field in StringToSign must be empty
        var dateValue = headers.ContainsKey("x-ms-date") ? "" : date.ToString();

        // Canonicalized x-ms-* headers (sorted alphabetically)
        var xmsHeaders = headers
            .Where(h => h.Key.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase))
            .OrderBy(h => h.Key.ToLowerInvariant())
            .Select(h => $"{h.Key.ToLowerInvariant()}:{h.Value}");
        var canonicalizedHeaders = string.Join("\n", xmsHeaders);

        var canonicalizedResource = "/" + storageAccountName + absolutePath;
        if (query.HasValue)
        {
            var queryParams = query.Value!.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .OrderBy(p => p[0])
                .Select(p => $"\n{p[0]}:{Uri.UnescapeDataString(p[1])}");
            canonicalizedResource += string.Concat(queryParams);
        }

        var contentLengthStr = contentLength.ToString() == "0" ? "" : contentLength.ToString();

        return string.Join("\n",
            method,
            contentEncoding,
            contentLanguage,
            contentLengthStr,
            contentMd5,
            contentType,
            dateValue,
            ifModifiedSince,
            ifMatch,
            ifNoneMatch,
            ifUnmodifiedSince,
            range,
            canonicalizedHeaders,
            canonicalizedResource);
    }

    private static string ComputeHmacSha256(string base64Key, string stringToSign)
    {
        var keyBytes = Convert.FromBase64String(base64Key);
        var dataBytes = Encoding.UTF8.GetBytes(stringToSign);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(dataBytes));
    }

    private bool IsAnonymousAccessAllowed(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string absolutePath,
        QueryString query,
        string method)
    {
        if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError($"Anonymous access denied: method '{method}' is not permitted without credentials.");
            return false;
        }

        // If a SAS signature is present treat this as a failed SAS request, not anonymous.
        if (query.HasValue)
        {
            var queryPairs = query.Value!.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2)[0]);
            if (queryPairs.Any(k => k.Equals("sig", StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogError("Authentication failure for Blob Storage: SAS signature present but no Authorization header.");
                return false;
            }
        }

        var segments = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return false;
        var containerName = segments[0];

        var accessLevel = _blobControlPlane.GetContainerPublicAccess(
            subscriptionIdentifier, resourceGroupIdentifier, storageAccountName, containerName);

        if (string.IsNullOrEmpty(accessLevel))
        {
            logger.LogError($"Anonymous access denied: container '{containerName}' has no public access configured.");
            return false;
        }

        var isBlobOperation = segments.Length > 1;

        if (accessLevel.Equals("container", StringComparison.OrdinalIgnoreCase))
        {
            // container level: allow list-blobs (comp=list) and all blob GET operations
            if (isBlobOperation) return true;
            if (query.HasValue)
            {
                var queryPairs = query.Value!.TrimStart('?')
                    .Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split('=', 2))
                    .Where(p => p.Length == 2)
                    .ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);
                if (queryPairs.TryGetValue("comp", out var comp) &&
                    comp.Equals("list", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            logger.LogError($"Anonymous access denied: container-level access does not permit this operation on '{absolutePath}'.");
            return false;
        }

        if (accessLevel.Equals("blob", StringComparison.OrdinalIgnoreCase))
        {
            if (isBlobOperation) return true;
            logger.LogError($"Anonymous access denied: blob-level access does not permit container operations on '{absolutePath}'.");
            return false;
        }

        logger.LogError($"Anonymous access denied: unrecognised access level '{accessLevel}' on container '{containerName}'.");
        return false;
    }
}
