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
/// Validates authorization for Azure Queue Storage data-plane requests.
/// Supports SharedKey (13-field Blob/Queue format), SharedKeyLite, and Bearer (RBAC).
/// </summary>
internal sealed class QueueStorageSecurityProvider(Pipeline eventPipeline, ITopazLogger logger)
{
    private readonly AzureStorageControlPlane _controlPlane = new(new StorageResourceProvider(logger), logger);
    private readonly StorageDataPlaneAuthorizationChecker _bearerChecker = new(eventPipeline, logger);

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
        if (!headers.TryGetValue("Authorization", out var value))
        {
            logger.LogError("Authentication failure for Queue Storage. Authorization header is missing.");
            return false;
        }

        var headerValue = value.ToString();
        var parts = headerValue.Split(' ', 2);
        var scheme = parts[0];

        logger.LogDebug(nameof(QueueStorageSecurityProvider), nameof(RequestIsAuthorized),
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
        logger.LogError($"Authentication failure for Queue Storage. Unsupported scheme: {scheme}.");
        return false;
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
            logger.LogError("Authentication failure for Queue SharedKey scheme. Header value format is incorrect.");
            return false;
        }

        var accountName = valueParts[0];
        var signature = valueParts[1];

        if (accountName != storageAccountName)
        {
            logger.LogError("Authentication failure for Queue SharedKey. Account name mismatch.");
            return false;
        }

        var storageAccountResult = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccountResult.Result != OperationResult.Success || storageAccountResult.Resource == null)
            throw new InvalidOperationException($"Storage account {storageAccountName} does not exist.");

        var stringToSign = BuildStringToSignForSharedKeyFull(storageAccountName, method, absolutePath, headers, query);

        logger.LogDebug(nameof(QueueStorageSecurityProvider), nameof(IsAuthorizedForSharedKey),
            "Path='{0}' StringToSign='{1}'", absolutePath, stringToSign.Replace("\n", "\\n"));

        var hash1 = ComputeHmacSha256(storageAccountResult.Resource.Keys[0].Value, stringToSign);
        var hash2 = ComputeHmacSha256(storageAccountResult.Resource.Keys[1].Value, stringToSign);

        if (hash1 == signature || hash2 == signature) return true;

        logger.LogError(nameof(QueueStorageSecurityProvider), nameof(IsAuthorizedForSharedKey),
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
            logger.LogError("Authentication failure for Queue SharedKeyLite scheme. Header value format is incorrect.");
            return false;
        }

        var accountName = valueParts[0];
        var signature = valueParts[1];

        if (accountName != storageAccountName)
        {
            logger.LogError("Authentication failure for Queue SharedKeyLite. Account name mismatch.");
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

        logger.LogError(nameof(QueueStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLite),
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

        var dateValue = headers.ContainsKey("x-ms-date") ? "" : date.ToString();

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
}
