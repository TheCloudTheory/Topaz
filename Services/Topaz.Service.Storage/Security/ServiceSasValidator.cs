using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.Storage.Security;

/// <summary>
/// Validates Azure Storage Service SAS tokens for Blob, Queue, and Table data-plane requests.
/// Verifies HMAC-SHA256 signature, expiry, and protocol. IP range enforcement (sip) is not
/// implemented and is treated as a known limitation — requests with sip set are logged and passed.
/// </summary>
internal sealed class ServiceSasValidator(AzureStorageControlPlane controlPlane, ITopazLogger logger)
{
    internal enum SasServiceType { Blob, Queue, Table }

    /// <summary>
    /// Returns true when the query string contains the minimum parameters that identify a Service SAS
    /// (sv= and sig= both present).
    /// </summary>
    public static bool IsServiceSas(QueryString query)
    {
        if (!query.HasValue) return false;
        var keys = query.Value!.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("sv") && keys.Contains("sig");
    }

    /// <summary>
    /// Validates a Service SAS request. Returns true when the signature is valid and the token
    /// has not expired.
    /// </summary>
    /// <param name="subscriptionIdentifier">Subscription that owns the storage account.</param>
    /// <param name="resourceGroupIdentifier">Resource group that owns the storage account.</param>
    /// <param name="storageAccountName">Name of the storage account.</param>
    /// <param name="absolutePath">Path from the request (without query string).</param>
    /// <param name="query">Full query string of the request.</param>
    /// <param name="serviceType">Blob, Queue, or Table.</param>
    /// <param name="method">HTTP method of the request (GET, PUT, POST, DELETE, etc.).</param>
    /// <param name="policyResolver">
    /// Called when si= is present; receives the policy ID and returns the stored access policy or
    /// null when the policy does not exist (which causes a 403).
    /// </param>
    public StorageAuthorizationResult Validate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string absolutePath,
        QueryString query,
        SasServiceType serviceType,
        string method,
        Func<string, StoredAccessPolicy?> policyResolver)
    {
        var parsed = HttpUtility.ParseQueryString(query.ToString());

        var sv  = parsed["sv"]  ?? string.Empty;
        var sr  = parsed["sr"]  ?? string.Empty;
        var sp  = parsed["sp"]  ?? string.Empty;
        var se  = parsed["se"]  ?? string.Empty;
        var st  = parsed["st"]  ?? string.Empty;
        var si  = parsed["si"]  ?? string.Empty;
        var sip = parsed["sip"] ?? string.Empty;
        var spr = parsed["spr"] ?? string.Empty;
        var sig = parsed["sig"] ?? string.Empty;

        // Response-header overrides (Blob only)
        var rscc = parsed["rscc"] ?? string.Empty;
        var rscd = parsed["rscd"] ?? string.Empty;
        var rsce = parsed["rsce"] ?? string.Empty;
        var rscl = parsed["rscl"] ?? string.Empty;
        var rsct = parsed["rsct"] ?? string.Empty;

        // Table partition/row key range (Table only)
        var spk = parsed["spk"] ?? string.Empty;
        var srk = parsed["srk"] ?? string.Empty;
        var epk = parsed["epk"] ?? string.Empty;
        var erk = parsed["erk"] ?? string.Empty;

        // If si= references a stored access policy, merge its fields.
        // Per the Azure SAS spec, fields that are stored in the access policy are represented
        // as empty strings in the string to sign — wire values take precedence, but the policy
        // fills in any that were omitted. Save wire values before merging so that the StringToSign
        // is always built from the original wire token (as the client signed it).
        var wireSignedPermissions = sp;
        var wireSignedStart       = st;
        var wireSignedExpiry      = se;

        if (!string.IsNullOrEmpty(si))
        {
            var policy = policyResolver(si);
            if (policy == null)
            {
                logger.LogError(nameof(ServiceSasValidator), nameof(Validate),
                    "Service SAS references stored policy '{0}' which does not exist on resource '{1}'. Denying.",
                    si, absolutePath);
                return StorageAuthorizationResult.AuthenticationFailed();
            }

            // Merge policy fields into sp/st/se for authorization checks (expiry).
            // These merged values are NOT used for the StringToSign.
            if (string.IsNullOrEmpty(sp) && !string.IsNullOrEmpty(policy.Permissions)) sp = policy.Permissions;
            if (string.IsNullOrEmpty(st) && !string.IsNullOrEmpty(policy.StartsOn))    st = policy.StartsOn;
            if (string.IsNullOrEmpty(se) && !string.IsNullOrEmpty(policy.ExpiresOn))   se = policy.ExpiresOn;
        }

        // Validate expiry.
        if (!string.IsNullOrEmpty(se))
        {
            if (!DateTimeOffset.TryParse(se, null, DateTimeStyles.AssumeUniversal, out var expiry))
            {
                logger.LogError(nameof(ServiceSasValidator), nameof(Validate),
                    "Service SAS has unparseable expiry '{0}'. Denying.", se);
                return StorageAuthorizationResult.AuthenticationFailed();
            }

            if (DateTimeOffset.UtcNow >= expiry)
            {
                logger.LogError(nameof(ServiceSasValidator), nameof(Validate),
                    "Service SAS token has expired (se={0}, now={1}). Denying.", se, DateTimeOffset.UtcNow);
                return StorageAuthorizationResult.AuthenticationFailed();
            }
        }

        // IP restriction — known limitation: sip is not enforced.
        if (!string.IsNullOrEmpty(sip))
        {
            logger.LogDebug(nameof(ServiceSasValidator), nameof(Validate),
                "Service SAS contains sip='{0}' restriction — IP enforcement is not implemented; passing.", sip);
        }

        // Validate that the HTTP method is covered by sp= permission letters (fail-fast before HMAC).
        if (!IsMethodPermitted(method, sp, serviceType))
        {
            logger.LogError(nameof(ServiceSasValidator), nameof(Validate),
                "Service SAS sp='{0}' does not cover HTTP method '{1}' for {2}. Denying.", sp, method, serviceType);
            return StorageAuthorizationResult.PermissionMismatch();
        }

        var canonicalizedResource = BuildCanonicalizedResource(storageAccountName, absolutePath, sr, serviceType);

        var stringToSign = serviceType switch
        {
            SasServiceType.Blob  => BuildBlobStringToSign(wireSignedPermissions, wireSignedStart, wireSignedExpiry, canonicalizedResource, si, sip, spr, sv, sr, rscc, rscd, rsce, rscl, rsct),
            SasServiceType.Queue => BuildQueueStringToSign(wireSignedPermissions, wireSignedStart, wireSignedExpiry, canonicalizedResource, si, sip, spr, sv),
            SasServiceType.Table => BuildTableStringToSign(wireSignedPermissions, wireSignedStart, wireSignedExpiry, canonicalizedResource, si, sip, spr, sv, spk, srk, epk, erk),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType))
        };

        logger.LogDebug(nameof(ServiceSasValidator), nameof(Validate),
            "ServiceType={0} sr='{1}' CanonicalizedResource='{2}' StringToSign='{3}'",
            serviceType, sr, canonicalizedResource, stringToSign.Replace("\n", "\\n"));

        var accountResult = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (accountResult.Result != OperationResult.Success || accountResult.Resource == null)
        {
            logger.LogError(nameof(ServiceSasValidator), nameof(Validate),
                "Storage account '{0}' not found during Service SAS validation.", storageAccountName);
            return StorageAuthorizationResult.AuthenticationFailed();
        }

        var hash1 = ComputeHmacSha256(accountResult.Resource.Keys[0].Value, stringToSign);
        var hash2 = ComputeHmacSha256(accountResult.Resource.Keys[1].Value, stringToSign);

        if (hash1 == sig || hash2 == sig) return StorageAuthorizationResult.Authorized();

        logger.LogError(nameof(ServiceSasValidator), nameof(Validate),
            "Service SAS signature mismatch for account '{0}'. Expected [{1}...] or [{2}...], got [{3}...].",
            storageAccountName,
            hash1.Length >= 10 ? hash1[..10] : hash1,
            hash2.Length >= 10 ? hash2[..10] : hash2,
            sig.Length >= 10 ? sig[..10] : sig);
        return StorageAuthorizationResult.AuthenticationFailed();
    }

    private static string BuildCanonicalizedResource(
        string accountName, string absolutePath, string sr, SasServiceType serviceType)
    {
        // Extract the first path segment (container / queue / table name).
        var segments = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var firstSegment = segments.Length > 0 ? segments[0] : string.Empty;

        // For Table, strip OData key predicates/query suffix — e.g. "SasTable()" → "SasTable".
        var parenIndex = serviceType == SasServiceType.Table ? firstSegment.IndexOf('(') : -1;
        var resourceName = parenIndex >= 0 ? firstSegment[..parenIndex] : firstSegment;

        return serviceType switch
        {
            SasServiceType.Blob when sr.Equals("c", StringComparison.OrdinalIgnoreCase)
                // Container names are always lowercase in Azure.
                => $"/blob/{accountName}/{resourceName.ToLowerInvariant()}",
            SasServiceType.Blob
                // sr=b/bs/bvd — include the full path after the account name; preserve blob name case.
                => $"/blob/{accountName}{absolutePath}",
            SasServiceType.Queue
                // Queue names are always lowercase in Azure.
                => $"/queue/{accountName}/{resourceName.ToLowerInvariant()}",
            SasServiceType.Table
                // Azure SDK lowercases the table name in GetCanonicalName (TableSasBuilder.cs).
                => $"/table/{accountName}/{resourceName.ToLowerInvariant()}",
            _ => $"/{accountName}{absolutePath}"
        };
    }

    // Blob StringToSign (Azure REST API 2020-12-06+):
    // permissions\nstart\nexpiry\ncanonicalized_resource\nsi\nsip\nspr\nsv\nsr\n\n\nrscc\nrscd\nrsce\nrscl\nrsct
    private static string BuildBlobStringToSign(
        string sp, string st, string se, string canonicalizedResource,
        string si, string sip, string spr, string sv, string sr,
        string rscc, string rscd, string rsce, string rscl, string rsct) =>
        string.Join("\n",
            sp, st, se, canonicalizedResource, si, sip, spr, sv,
            sr,           // signedResource (b / c / bs / bv)
            string.Empty, // signedSnapshotTime
            string.Empty, // signedEncryptionScope
            rscc, rscd, rsce, rscl, rsct);

    // Queue StringToSign:
    // permissions\nstart\nexpiry\ncanonicalized_resource\nsi\nsip\nspr\nsv
    private static string BuildQueueStringToSign(
        string sp, string st, string se, string canonicalizedResource,
        string si, string sip, string spr, string sv) =>
        string.Join("\n", sp, st, se, canonicalizedResource, si, sip, spr, sv);

    // Table StringToSign:
    // permissions\nstart\nexpiry\ncanonicalized_resource\nsi\nsip\nspr\nsv\nspk\nsrk\nepk\nerk
    private static string BuildTableStringToSign(
        string sp, string st, string se, string canonicalizedResource,
        string si, string sip, string spr, string sv,
        string spk, string srk, string epk, string erk) =>
        string.Join("\n", sp, st, se, canonicalizedResource, si, sip, spr, sv,
            spk, srk, epk, erk);

    private static string ComputeHmacSha256(string base64Key, string stringToSign)
    {
        var keyBytes = Convert.FromBase64String(base64Key);
        var dataBytes = Encoding.UTF8.GetBytes(stringToSign);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(dataBytes));
    }

    /// <summary>
    /// Returns true when the HTTP method is covered by the Service SAS permission letters in sp=.
    /// Permission mapping differs by service type (Blob, Queue, Table).
    /// 
    /// Blob permission mapping:
    ///   r (Read)       → GET, HEAD
    ///   w (Write)      → PUT
    ///   d (Delete)     → DELETE
    ///   l (List)       → GET
    ///   a (Add)        → POST, PUT
    ///   c (Create)     → PUT
    ///   p (Process)    → (not applicable to Blob)
    ///   u (Update)     → (not applicable to Blob)
    ///   q (Query)      → (not applicable to Blob)
    ///   t (Tags)       → GET, PUT, HEAD
    ///   x (Delete version) → DELETE
    ///   f (Filter)     → GET
    ///   e (Execute)    → GET
    ///   m (Modify)     → PUT
    ///   i (Immutable)  → PUT, DELETE
    ///
    /// Queue permission mapping:
    ///   r (Read)       → GET, HEAD
    ///   w (Write)      → (not applicable to Queue)
    ///   d (Delete)     → DELETE
    ///   l (List)       → GET
    ///   a (Add)        → POST
    ///   c (Create)     → (not applicable to Queue)
    ///   p (Process)    → GET, DELETE
    ///   u (Update)     → PUT
    ///   q (Query)      → (not applicable to Queue)
    ///   t, x, f, e, m, i → (not applicable to Queue)
    ///
    /// Table permission mapping:
    ///   r (Read)       → GET, HEAD
    ///   w (Write)      → (not applicable to Table)
    ///   d (Delete)     → DELETE
    ///   l (List)       → GET
    ///   a (Add)        → POST
    ///   c (Create)     → (not applicable to Table)
    ///   p (Process)    → (not applicable to Table)
    ///   u (Update)     → PUT, MERGE, PATCH
    ///   q (Query)      → GET
    ///   t, x, f, e, m, i → (not applicable to Table)
    /// </summary>
    private static bool IsMethodPermitted(string method, string sp, SasServiceType serviceType)
    {
        if (string.IsNullOrEmpty(sp)) return false;

        var upperMethod = method.ToUpperInvariant();

        return serviceType switch
        {
            SasServiceType.Blob => upperMethod switch
            {
                "GET"    => sp.IndexOfAny(['r', 'l', 'f', 'e', 't']) >= 0,
                "HEAD"   => sp.IndexOfAny(['r', 't']) >= 0,
                "PUT"    => sp.IndexOfAny(['w', 'c', 'a', 'm', 't', 'i']) >= 0,
                "POST"   => sp.IndexOfAny(['a']) >= 0,
                "DELETE" => sp.IndexOfAny(['d', 'x', 'i']) >= 0,
                "PATCH"  => false,  // Blob does not support PATCH in standard operations
                "MERGE"  => false,  // Blob does not support MERGE
                _        => false
            },
            SasServiceType.Queue => upperMethod switch
            {
                "GET"    => sp.IndexOfAny(['r', 'p', 'l']) >= 0,
                "HEAD"   => sp.IndexOfAny(['r']) >= 0,
                "PUT"    => sp.IndexOfAny(['u']) >= 0,
                "POST"   => sp.IndexOfAny(['a']) >= 0,
                "DELETE" => sp.IndexOfAny(['d', 'p']) >= 0,
                "PATCH"  => false,  // Queue does not support PATCH
                "MERGE"  => false,  // Queue does not support MERGE
                _        => false
            },
            SasServiceType.Table => upperMethod switch
            {
                "GET"    => sp.IndexOfAny(['r', 'l', 'q']) >= 0,
                "HEAD"   => sp.IndexOfAny(['r']) >= 0,
                "PUT"    => sp.IndexOfAny(['u']) >= 0,
                "POST"   => sp.IndexOfAny(['a']) >= 0,
                "DELETE" => sp.IndexOfAny(['d']) >= 0,
                "PATCH"  => sp.IndexOfAny(['u']) >= 0,
                "MERGE"  => sp.IndexOfAny(['u']) >= 0,
                _        => false
            },
            _ => false
        };
    }
}
