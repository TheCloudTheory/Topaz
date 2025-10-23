using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Security;

internal sealed class TableStorageSecurityProvider(ITopazLogger logger)
{
    private readonly AzureStorageControlPlane _controlPlane = new(new ResourceProvider(logger), logger);

    public bool RequestIsAuthorized(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, IHeaderDictionary headers,
        string absolutePath,
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
            case "SharedKeyLite":
                return IsAuthorizedForSharedKeyLiteScheme(subscriptionIdentifier, resourceGroupIdentifier,
                    storageAccountName, parts[1], headers, absolutePath, query);
            default:
                logger.LogError($"Authentication failure for {scheme}. Scheme is not supported.");
                return false;
        }
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
            logger.LogDebug($"[{nameof(IsAuthorizedForSharedKeyLiteScheme)}]: Header value: {headerValue}");
            
            return false;    
        }
        
        var accountName = valueParts[0];
        var signature = valueParts[1];
        
        // If the storage account name send along with the signature isn't correct,
        // the request must be considered as unauthorized
        if (accountName != storageAccountName)
        {
            logger.LogError($"Authentication failure for SharedKeyLite scheme. Storage account name isn't correct.");
            logger.LogDebug($"[{nameof(IsAuthorizedForSharedKeyLiteScheme)}]: Storage account name: {storageAccountName} but the value sent with the Authorization header is {accountName}.");
            
            return false;   
        }

        var storageAccountResource = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (storageAccountResource.result != OperationResult.Success || storageAccountResource.resource == null)
        {
            throw new InvalidOperationException($"Storage account {storageAccountName} does not exist.");
        }

        var stringToSign = BuildStringToSign(storageAccountName, absolutePath, headers, query);
        
        // Calculate hashes for both keys as we don't know which key was used for 
        // sending a request
        var hash1 = ComputeHashForKey(storageAccountResource.resource.Keys[0].Value, stringToSign);
        var hash2 = ComputeHashForKey(storageAccountResource.resource.Keys[1].Value, stringToSign);
        
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
        // 2. Append the resource's encoded URI path. If the request URI addresses a component of the resource,
        //    append the appropriate query string. The query string should include the question mark and the comp parameter
        //    (for example, ?comp=metadata). No other parameters should be included on the query string.
        
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