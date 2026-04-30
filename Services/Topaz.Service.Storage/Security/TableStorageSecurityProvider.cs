using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Identity;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Security;

internal sealed class TableStorageSecurityProvider(ITopazLogger logger)
{
    private readonly AzureStorageControlPlane _controlPlane = new(new StorageResourceProvider(logger), logger);

    public bool RequestIsAuthorized(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, IHeaderDictionary headers,
        string method, string absolutePath,
        QueryString query)
    {
        if (!headers.TryGetValue("Authorization", out var value))
        {
            logger.LogError($"Authentication failure for SharedKeyLite scheme. Authorization header is missing.");
            return false;
        }

        var headerValue = value.ToString();
        var parts = headerValue.Split(' ');
        var scheme = parts[0];

        logger.LogDebug(nameof(TableStorageSecurityProvider), nameof(RequestIsAuthorized),
            "Scheme='{0}' Method='{1}' Path='{2}'", scheme, method, absolutePath);

        switch (scheme)
        {
            case "SharedKey":
                return IsAuthorizedForSharedKeyScheme(subscriptionIdentifier, resourceGroupIdentifier,
                    storageAccountName, parts[1], headers, method, absolutePath, query);
            case "SharedKeyLite":
                return IsAuthorizedForSharedKeyLiteScheme(subscriptionIdentifier, resourceGroupIdentifier,
                    storageAccountName, parts[1], headers, absolutePath, query);
            case "Bearer":
                return IsAuthorizedForBearerScheme(headerValue);
            default:
                logger.LogError($"Authentication failure for {scheme}. Scheme is not supported.");
                return false;
        }
    }

    private bool IsAuthorizedForSharedKeyScheme(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string headerValue,
        IHeaderDictionary headers, string method, string absolutePath, QueryString query)
    {
        var valueParts = headerValue.Split(':');
        if (valueParts.Length != 2)
        {
            logger.LogError($"Authentication failure for SharedKey scheme. Header value isn't correct.");
            return false;
        }

        var accountName = valueParts[0];
        var signature = valueParts[1];

        if (accountName != storageAccountName)
        {
            logger.LogError($"Authentication failure for SharedKey scheme. Storage account name isn't correct.");
            return false;
        }

        var storageAccountResource = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccountResource.Result != OperationResult.Success || storageAccountResource.Resource == null)
            throw new InvalidOperationException($"Storage account {storageAccountName} does not exist.");

        var stringToSign = BuildStringToSignForSharedKey(storageAccountName, method, absolutePath, headers, query);

        logger.LogDebug(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "Path='{0}' StringToSign='{1}'", absolutePath, stringToSign.Replace("\n", "\\n"));

        var hash1 = ComputeHashForKey(storageAccountResource.Resource.Keys[0].Value, stringToSign);
        var hash2 = ComputeHashForKey(storageAccountResource.Resource.Keys[1].Value, stringToSign);

        logger.LogDebug(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "Match={0} hash1={1} received={2}", hash1 == signature, hash1[..10], signature[..10]);

        if (hash1 == signature) return true;
        if (hash2 == signature) return true;

        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "Signature mismatch for account '{0}'", storageAccountName);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  Method:            {0}", method);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  Path:              {0}", absolutePath);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  StringToSign:      {0}", stringToSign.Replace("\n", "\\n"));
        headers.TryGetValue("x-ms-date", out var xmsdate);
        headers.TryGetValue("Date", out var dateHdr);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  x-ms-date hdr:     {0}", xmsdate.ToString());
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  Date hdr:          {0}", dateHdr.ToString());
        // Log full Authorization header — the truncated version was hiding useful detail
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  AuthHeaderFull:    {0}", headers["Authorization"].ToString());
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  ReceivedSignature: {0}", signature);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  ComputedHash(key1):{0}", hash1);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  ComputedHash(key2):{0}", hash2);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  StoredKey1Prefix:  {0}", storageAccountResource.Resource.Keys[0].Value[..16]);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  StoredKey1Full:    {0}", storageAccountResource.Resource.Keys[0].Value);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  StoredKey2Full:    {0}", storageAccountResource.Resource.Keys[1].Value);

        // Log key bytes as hex to confirm base64 decoding yields the expected raw bytes
        var key1Bytes = Convert.FromBase64String(storageAccountResource.Resource.Keys[0].Value);
        var key2Bytes = Convert.FromBase64String(storageAccountResource.Resource.Keys[1].Value);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  Key1BytesHex:      {0}", Convert.ToHexString(key1Bytes));
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  Key2BytesHex:      {0}", Convert.ToHexString(key2Bytes));

        // Dump ALL request headers (name + value + value-bytes-hex) for complete diagnostic context
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  AllRequestHeaders:");
        foreach (var header in headers)
        {
            var headerValStr = header.Value.ToString();
            var headerValHex = Convert.ToHexString(Encoding.UTF8.GetBytes(headerValStr));
            logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
                "    {0}: '{1}' [hex:{2}]", header.Key, headerValStr, headerValHex);
        }

        // Log raw STS bytes to detect invisible characters (BOM, \r, etc.)
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  STS_hex:           {0}", Convert.ToHexString(Encoding.UTF8.GetBytes(stringToSign)));

        // Try with URL-decoded path (space instead of %20) — tests whether the
        // client signed with the decoded path while Topaz received the encoded form.
        var decodedPath = Uri.UnescapeDataString(absolutePath);
        if (decodedPath != absolutePath)
        {
            var stsDecoded = BuildStringToSignForSharedKey(storageAccountName, method, decodedPath, headers, query);
            var hashDecoded1 = ComputeHashForKey(storageAccountResource.Resource.Keys[0].Value, stsDecoded);
            var hashDecoded2 = ComputeHashForKey(storageAccountResource.Resource.Keys[1].Value, stsDecoded);
            logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
                "  DecodedPathSTS:    {0}", stsDecoded.Replace("\n", "\\n"));
            logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
                "  DecodedHash(k1):   {0}  match={1}", hashDecoded1, hashDecoded1 == signature);
            logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
                "  DecodedHash(k2):   {0}  match={1}", hashDecoded2, hashDecoded2 == signature);
        }

        // Try the full 13-field SharedKey (Blob/Queue style) StringToSign to check whether
        // the client might be using that format instead of the 4-field SharedKeyTable format.
        var fullSts = BuildStringToSignForSharedKeyFull(storageAccountName, method, absolutePath, headers, query);
        var fullHash1 = ComputeHashForKey(storageAccountResource.Resource.Keys[0].Value, fullSts);
        var fullHash2 = ComputeHashForKey(storageAccountResource.Resource.Keys[1].Value, fullSts);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  FullSTS(13-field): {0}", fullSts.Replace("\n", "\\n"));
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  FullHash(key1):    {0}  match={1}", fullHash1, fullHash1 == signature);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyScheme),
            "  FullHash(key2):    {0}  match={1}", fullHash2, fullHash2 == signature);
        return false;
    }

    private bool IsAuthorizedForBearerScheme(string authHeader)
    {
        var token = JwtHelper.ValidateJwt(authHeader);
        if (token == null)
        {
            logger.LogError("Authentication failure for Bearer scheme. JWT validation failed.");
            return false;
        }

        return true;
    }

    private string BuildStringToSignForSharedKey(string storageAccountName, string method, string absolutePath,
        IHeaderDictionary headers, QueryString query)
    {
        headers.TryGetValue("Content-MD5", out var contentMd5);
        headers.TryGetValue("Content-Type", out var contentType);
        headers.TryGetValue("x-ms-date", out var date);

        var canonicalizedResource = "/" + storageAccountName + absolutePath;
        if (query.TryGetValueForKey("comp", out var comp))
            canonicalizedResource += "?comp=" + comp;

        return method + "\n" + contentMd5.ToString() + "\n" + contentType.ToString() + "\n" +
               date.ToString() + "\n" + canonicalizedResource;
    }

    private bool IsAuthorizedForSharedKeyLiteScheme(SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string headerValue,
        IHeaderDictionary headers, string absolutePath, QueryString query)
    {
        // SharedKeyLite authorization header value looks like this:
        // Authorization: SharedKeyLite myaccount:ctzMq410TV3wS7upTBcunJTDLEJwMAZuFPfr0mrrA08=
        // 
        // The signature is encoded based on the following logic:
        // StringToSign = Date + "\n"CanonicalizedResource
        //
        // To construct the header, SDK (or a client in general) must use HMAC-SHA256 algorithm,
        // encode the string append it to the value.
        
        // First, let's validate if the syntax of the header is correct
        var valueParts = headerValue.Split(':');
        if (valueParts.Length != 2)
        {
            logger.LogError($"Authentication failure for SharedKeyLite scheme. Header value isn't correct.");
            logger.LogDebug(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme), "Header value: {0}", headerValue);
            
            return false;    
        }
        
        var accountName = valueParts[0];
        var signature = valueParts[1];
        
        // If the storage account name send along with the signature isn't correct,
        // the request must be considered as unauthorized
        if (accountName != storageAccountName)
        {
            logger.LogError($"Authentication failure for SharedKeyLite scheme. Storage account name isn't correct.");
            logger.LogDebug(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme), "Storage account name: {0} but the value sent with the Authorization header is {1}.", storageAccountName, accountName);
            
            return false;   
        }

        var storageAccountResource = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccountResource.Result != OperationResult.Success || storageAccountResource.Resource == null)
        {
            throw new InvalidOperationException($"Storage account {storageAccountName} does not exist.");
        }

        var stringToSign = BuildStringToSign(storageAccountName, absolutePath, headers, query);
        
        logger.LogDebug(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme),
            "StringToSign (base64): {0}", Convert.ToBase64String(Encoding.UTF8.GetBytes(stringToSign)));
        
        // Calculate hashes for both keys as we don't know which key was used for 
        // sending a request
        var hash1 = ComputeHashForKey(storageAccountResource.Resource.Keys[0].Value, stringToSign);
        var hash2 = ComputeHashForKey(storageAccountResource.Resource.Keys[1].Value, stringToSign);
        
        logger.LogDebug(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme),
            "Computed hash1: {0}, Received: {1}, Match: {2}", hash1, signature, hash1 == signature);
        
        if (hash1 == signature) return true;
        if (hash2 == signature) return true;

        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme),
            "Signature mismatch for account '{0}'", storageAccountName);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme),
            "  Path:              {0}", absolutePath);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme),
            "  StringToSign:      {0}", stringToSign.Replace("\n", "\\n"));
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme),
            "  ReceivedSignature: {0}", signature);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme),
            "  ComputedHash(key1):{0}", hash1);
        logger.LogError(nameof(TableStorageSecurityProvider), nameof(IsAuthorizedForSharedKeyLiteScheme),
            "  ComputedHash(key2):{0}", hash2);
        return false;
    }
    
    private string BuildStringToSign(string storageAccountName, string absolutePath, IHeaderDictionary headers,
        QueryString query)
    {
        headers.TryGetValue("x-ms-date", out var date);

        var stringToSign = string.Join("\n",
            date,
            BuildCanonicalizedResource(storageAccountName, absolutePath, query));
        
        return stringToSign;
    }

    private string BuildCanonicalizedResource(string storageAccountName, string actualPath, QueryString query)
    {
        // To calculate `CanonicalizedResource` for Table Storage we need to do a bunch of additional steps:
        // 1. Beginning with an empty string (""), append a forward slash (/), followed by the name of the account
        //    that owns the resource being accessed.
        // 2. Append the raw (percent-encoded) resource URI path as received. The go-azure-sdk used by the
        //    Terraform azurerm provider computes its signature over the raw path (e.g. %20, not a literal space),
        //    so we must use the same form for the HMAC to match.
        // 3. If the request URI addresses a component of the resource, append the appropriate query string.
        //    The query string should include the question mark and the comp parameter (for example, ?comp=metadata).
        //    No other parameters should be included on the query string.
        
        var canonicalizedResource = "/" + storageAccountName;
        canonicalizedResource += actualPath;
        
        if (query.TryGetValueForKey("comp", out var comp))
        {
            canonicalizedResource += "?comp=" + comp;
        }
        
        return canonicalizedResource;
    }

    private string ComputeHashForKey(string accountKey, string canonicalizedResource)
    {
        using var hmac = new HMACSHA256(Convert.FromBase64String(accountKey));
        
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalizedResource));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Builds the full 13-field SharedKey StringToSign used for Blob/Queue service requests.
    /// This is different from the 4-field SharedKeyTable format. Logged for diagnostic purposes only
    /// to determine whether the client is inadvertently using the wrong format.
    /// </summary>
    private string BuildStringToSignForSharedKeyFull(string storageAccountName, string method, string absolutePath,
        IHeaderDictionary headers, QueryString query)
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

        // When x-ms-date is present, the Date field in StringToSign must be empty
        var dateValue = headers.ContainsKey("x-ms-date") ? "" : date.ToString();

        // Canonicalized x-ms-* headers (sorted alphabetically)
        var xmsHeaders = headers
            .Where(h => h.Key.StartsWith("x-ms-", StringComparison.OrdinalIgnoreCase))
            .OrderBy(h => h.Key.ToLowerInvariant())
            .Select(h => $"{h.Key.ToLowerInvariant()}:{h.Value}");
        var canonicalizedHeaders = string.Join("\n", xmsHeaders);

        var canonicalizedResource = "/" + storageAccountName + absolutePath;
        // For full SharedKey, ALL query params are included (sorted)
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
}