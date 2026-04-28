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

        var hash1 = ComputeHashForKey(storageAccountResource.Resource.Keys[0].Value, stringToSign);
        var hash2 = ComputeHashForKey(storageAccountResource.Resource.Keys[1].Value, stringToSign);

        if (hash1 == signature) return true;
        return hash2 == signature;
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
        return hash2 == signature;
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
}