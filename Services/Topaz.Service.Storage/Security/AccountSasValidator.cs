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
/// Validates Azure Storage Account SAS tokens for Blob, Queue, and Table data-plane requests.
/// Account SAS tokens are distinguished from Service SAS tokens by the presence of the ss=
/// (signed services) and srt= (signed resource types) parameters.
/// Verifies HMAC-SHA256 signature, expiry, service letter (ss=), resource-type letter (srt=),
/// and HTTP method coverage (sp=). IP range enforcement (sip=) is a known limitation and is
/// logged but not enforced.
/// </summary>
internal sealed class AccountSasValidator(AzureStorageControlPlane controlPlane, ITopazLogger logger)
{
    /// <summary>Identifies the Azure storage service being accessed.</summary>
    internal enum AccountSasService { Blob = 'b', Queue = 'q', Table = 't', File = 'f' }

    /// <summary>Identifies the Account SAS resource-type scope per the Azure REST API spec.</summary>
    internal enum AccountSasResourceType { Service = 's', Container = 'c', Object = 'o' }

    /// <summary>
    /// Returns true when the query string contains all four parameters that identify an Account SAS:
    /// sv= (version), sig= (signature), ss= (signed services), and srt= (signed resource types).
    /// The ss= and srt= parameters are unique to Account SAS and distinguish it from Service SAS,
    /// which has neither.
    /// </summary>
    public static bool IsAccountSas(QueryString query)
    {
        if (!query.HasValue) return false;
        var keys = query.Value!.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("sv") && keys.Contains("sig")
               && keys.Contains("ss") && keys.Contains("srt");
    }

    /// <summary>
    /// Validates an Account SAS token. Returns true when all of the following hold:
    /// <list type="bullet">
    ///   <item>The HMAC-SHA256 signature matches a current account key.</item>
    ///   <item>The token has not expired (se=).</item>
    ///   <item>The required service letter is present in ss=.</item>
    ///   <item>The required resource-type letter is present in srt=.</item>
    ///   <item>The HTTP method is covered by sp= permissions.</item>
    /// </list>
    /// </summary>
    /// <param name="subscriptionIdentifier">Subscription that owns the storage account.</param>
    /// <param name="resourceGroupIdentifier">Resource group that owns the storage account.</param>
    /// <param name="storageAccountName">Storage account name.</param>
    /// <param name="method">HTTP method of the incoming request (GET, PUT, DELETE, …).</param>
    /// <param name="query">Full query string of the request, including all SAS parameters.</param>
    /// <param name="requiredService">The service letter that must appear in ss=.</param>
    /// <param name="requiredResourceType">The resource-type letter that must appear in srt=.</param>
    public bool Validate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string method,
        QueryString query,
        AccountSasService requiredService,
        AccountSasResourceType requiredResourceType)
    {
        var parsed = HttpUtility.ParseQueryString(query.ToString());

        var sv  = parsed["sv"]  ?? string.Empty;
        var ss  = parsed["ss"]  ?? string.Empty;
        var srt = parsed["srt"] ?? string.Empty;
        var sp  = parsed["sp"]  ?? string.Empty;
        var se  = parsed["se"]  ?? string.Empty;
        var st  = parsed["st"]  ?? string.Empty;
        var sip = parsed["sip"] ?? string.Empty;
        var spr = parsed["spr"] ?? string.Empty;
        var sig = parsed["sig"] ?? string.Empty;

        // Validate expiry.
        if (!string.IsNullOrEmpty(se))
        {
            if (!DateTimeOffset.TryParse(se, null, DateTimeStyles.AssumeUniversal, out var expiry))
            {
                logger.LogError(nameof(AccountSasValidator), nameof(Validate),
                    "Account SAS has unparseable expiry '{0}'. Denying.", se);
                return false;
            }

            if (DateTimeOffset.UtcNow >= expiry)
            {
                logger.LogError(nameof(AccountSasValidator), nameof(Validate),
                    "Account SAS token has expired (se={0}, now={1}). Denying.", se, DateTimeOffset.UtcNow);
                return false;
            }
        }

        // IP restriction — known limitation: sip is not enforced.
        if (!string.IsNullOrEmpty(sip))
        {
            logger.LogDebug(nameof(AccountSasValidator), nameof(Validate),
                "Account SAS contains sip='{0}' restriction — IP enforcement is not implemented; passing.", sip);
        }

        // Validate that the required service letter is in ss=.
        var serviceChar = (char)requiredService;
        if (!ss.Contains(serviceChar, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(nameof(AccountSasValidator), nameof(Validate),
                "Account SAS ss='{0}' does not include required service '{1}'. Denying.", ss, serviceChar);
            return false;
        }

        // Validate that the required resource-type letter is in srt=.
        var resourceTypeChar = (char)requiredResourceType;
        if (!srt.Contains(resourceTypeChar, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError(nameof(AccountSasValidator), nameof(Validate),
                "Account SAS srt='{0}' does not include required resource type '{1}'. Denying.", srt, resourceTypeChar);
            return false;
        }

        // Validate that the HTTP method is covered by sp= permission letters.
        if (!IsMethodPermitted(method, sp))
        {
            logger.LogError(nameof(AccountSasValidator), nameof(Validate),
                "Account SAS sp='{0}' does not cover HTTP method '{1}'. Denying.", sp, method);
            return false;
        }

        // Build StringToSign.
        // For sv >= 2020-12-06 the spec adds a signed-encryption-scope (ses) field after sv.
        // https://learn.microsoft.com/rest/api/storageservices/create-account-sas
        var ses = parsed["ses"] ?? string.Empty;
        var includeEncryptionScope = IsVersionAtLeast(sv, 2020, 12, 6);

        // Format (pre-2020-12-06): accountName\nsp\nss\nsrt\nst\nse\nsip\nspr\nsv\n
        // Format (2020-12-06+):    accountName\nsp\nss\nsrt\nst\nse\nsip\nspr\nsv\nses\n
        var stringToSign = includeEncryptionScope
            ? string.Join("\n", storageAccountName, sp, ss, srt, st, se, sip, spr, sv, ses, string.Empty)
            : string.Join("\n", storageAccountName, sp, ss, srt, st, se, sip, spr, sv, string.Empty);

        logger.LogDebug(nameof(AccountSasValidator), nameof(Validate),
            "Account='{0}' Service='{1}' ResourceType='{2}' StringToSign='{3}'",
            storageAccountName, serviceChar, resourceTypeChar, stringToSign.Replace("\n", "\\n"));

        var accountResult = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (accountResult.Result != OperationResult.Success || accountResult.Resource == null)
        {
            logger.LogError(nameof(AccountSasValidator), nameof(Validate),
                "Storage account '{0}' not found during Account SAS validation.", storageAccountName);
            return false;
        }

        var dataBytes = Encoding.UTF8.GetBytes(stringToSign);
        var hash1 = Convert.ToBase64String(
            HMACSHA256.HashData(Convert.FromBase64String(accountResult.Resource.Keys[0].Value), dataBytes));
        var hash2 = Convert.ToBase64String(
            HMACSHA256.HashData(Convert.FromBase64String(accountResult.Resource.Keys[1].Value), dataBytes));

        if (hash1 == sig || hash2 == sig) return true;

        logger.LogError(nameof(AccountSasValidator), nameof(Validate),
            "Account SAS signature mismatch for account '{0}'. Expected [{1}...] or [{2}...], got [{3}...].",
            storageAccountName,
            hash1.Length >= 10 ? hash1[..10] : hash1,
            hash2.Length >= 10 ? hash2[..10] : hash2,
            sig.Length >= 10 ? sig[..10] : sig);
        return false;
    }

    /// <summary>
    /// Convenience overload that derives the <see cref="AccountSasResourceType"/> from the URL
    /// path depth, so callers do not need to duplicate the mapping logic. Rules:
    /// <list type="bullet">
    ///   <item>0 path segments → <see cref="AccountSasResourceType.Service"/></item>
    ///   <item>Blob / Queue, 1 segment → <see cref="AccountSasResourceType.Container"/></item>
    ///   <item>Blob / Queue, 2+ segments → <see cref="AccountSasResourceType.Object"/></item>
    ///   <item>Table, 1+ segments → <see cref="AccountSasResourceType.Object"/> (Table has no
    ///     container-scope; tables and their entities both map to Object)</item>
    /// </list>
    /// </summary>
    public bool ValidateForPath(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string method,
        string absolutePath,
        QueryString query,
        AccountSasService service)
    {
        var segments = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resourceType = service == AccountSasService.Table
            ? (segments.Length == 0 ? AccountSasResourceType.Service : AccountSasResourceType.Object)
            : segments.Length switch
            {
                0 => AccountSasResourceType.Service,
                1 => AccountSasResourceType.Container,
                _ => AccountSasResourceType.Object
            };
        return Validate(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
            method, query, service, resourceType);
    }

    /// <summary>
    /// Returns true when the HTTP method is covered by the Account SAS permission letters in sp=.
    /// Permission mapping (per Azure REST API docs):
    ///   r (Read)            → GET, HEAD
    ///   w (Write)           → PUT
    ///   d (Delete)          → DELETE
    ///   l (List)            → GET  (list operations)
    ///   a (Add)             → POST (e.g. add queue message)
    ///   c (Create)          → PUT, POST
    ///   u (Update)          → PUT, PATCH, MERGE
    ///   p (Process)         → GET  (dequeue messages)
    ///   t (Tag / Table?)    → PUT, GET
    /// </summary>
    private static bool IsMethodPermitted(string method, string sp)
    {
        if (string.IsNullOrEmpty(sp)) return false;
        return method.ToUpperInvariant() switch
        {
            "GET"    => sp.IndexOfAny(['r', 'l', 'p']) >= 0,
            "HEAD"   => sp.IndexOfAny(['r']) >= 0,
            "PUT"    => sp.IndexOfAny(['w', 'c', 'u']) >= 0,
            "POST"   => sp.IndexOfAny(['a', 'w', 'c', 'p']) >= 0,
            "DELETE" => sp.IndexOfAny(['d']) >= 0,
            "PATCH"  => sp.IndexOfAny(['u', 'w']) >= 0,
            "MERGE"  => sp.IndexOfAny(['u', 'w']) >= 0,
            _        => false
        };
    }

    /// <summary>
    /// Returns true if the version string (e.g. "2025-05-05") represents a date on or after
    /// the given year, month, and day. Non-parseable version strings return false.
    /// </summary>
    private static bool IsVersionAtLeast(string version, int year, int month, int day)
    {
        if (!DateOnly.TryParseExact(version, "yyyy-MM-dd", null, DateTimeStyles.None, out var v))
            return false;
        return v >= new DateOnly(year, month, day);
    }
}
