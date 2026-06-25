using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.Storage.Security;

/// <summary>
/// Validates Azure Storage User Delegation SAS tokens for Blob data-plane requests.
/// User Delegation SAS tokens are signed with a user delegation key derived deterministically from
/// the storage account key and the caller's Entra ID identity fields (skoid, sktid, skt, ske, sks,
/// skv). The same derivation is used at validation time so no key persistence is required.
/// IP range enforcement (sip=) is implemented via <see cref="SipIpRangeChecker"/>.
/// </summary>
internal sealed class UserDelegationSasValidator(AzureStorageControlPlane controlPlane, ITopazLogger logger)
{
    /// <summary>
    /// Returns true when the query string contains the minimum parameters that identify a User
    /// Delegation SAS: sv=, sig=, and skoid= must all be present.
    /// skoid= (signed key object ID) discriminates User Delegation SAS from Service SAS, which
    /// never carries that parameter.
    /// </summary>
    public static bool IsUserDelegationSas(QueryString query)
    {
        if (!query.HasValue) return false;
        var keys = query.Value!.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2)[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Contains("sv") && keys.Contains("sig") && keys.Contains("skoid");
    }

    /// <summary>
    /// Validates a User Delegation SAS token. Returns an authorized result when the HMAC-SHA256
    /// signature is valid, the token has not expired, and the caller IP matches the sip= restriction.
    /// </summary>
    public StorageAuthorizationResult Validate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string absolutePath,
        QueryString query,
        IPAddress? remoteIpAddress)
    {
        var parsed = HttpUtility.ParseQueryString(query.ToString());

        var sv    = parsed["sv"]    ?? string.Empty;
        var sr    = parsed["sr"]    ?? string.Empty;
        var sp    = parsed["sp"]    ?? string.Empty;
        var se    = parsed["se"]    ?? string.Empty;
        var st    = parsed["st"]    ?? string.Empty;
        var sip   = parsed["sip"]   ?? string.Empty;
        var spr   = parsed["spr"]   ?? string.Empty;
        var sig   = parsed["sig"]   ?? string.Empty;
        var skoid = parsed["skoid"] ?? string.Empty;
        var sktid = parsed["sktid"] ?? string.Empty;
        var skt   = parsed["skt"]   ?? string.Empty;
        var ske   = parsed["ske"]   ?? string.Empty;
        var sks   = parsed["sks"]   ?? string.Empty;
        var skv   = parsed["skv"]   ?? string.Empty;
        var saoid  = parsed["saoid"]  ?? string.Empty;
        var suoid  = parsed["suoid"]  ?? string.Empty;
        var scid   = parsed["scid"]   ?? string.Empty;
        var skdtid = parsed["skdtid"] ?? string.Empty; // SignedDelegatedUserTenantId (added in service version 2025-07-05)
        var soid   = parsed["soid"]   ?? string.Empty; // DelegatedUserObjectId (added in service version 2025-07-05)

        // Response-header overrides (Blob only)
        var rscc = parsed["rscc"] ?? string.Empty;
        var rscd = parsed["rscd"] ?? string.Empty;
        var rsce = parsed["rsce"] ?? string.Empty;
        var rscl = parsed["rscl"] ?? string.Empty;
        var rsct = parsed["rsct"] ?? string.Empty;

        // Validate expiry.
        if (!string.IsNullOrEmpty(se))
        {
            if (!DateTimeOffset.TryParse(se, null, DateTimeStyles.AssumeUniversal, out var expiry))
            {
                logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
                    "User Delegation SAS has unparseable expiry '{0}'. Denying.", se);
                return StorageAuthorizationResult.AuthenticationFailed();
            }

            if (DateTimeOffset.UtcNow >= expiry)
            {
                logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
                    "User Delegation SAS has expired (se={0}, now={1}). Denying.", se, DateTimeOffset.UtcNow);
                return StorageAuthorizationResult.AuthenticationFailed();
            }
        }

        // IP restriction — enforce sip= (source IP range) if present.
        if (!string.IsNullOrEmpty(sip))
        {
            if (!SipIpRangeChecker.IsAllowed(sip, remoteIpAddress))
            {
                logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
                    "User Delegation SAS sip='{0}' does not allow caller IP '{1}'. Denying.",
                    sip, remoteIpAddress?.ToString() ?? "(null)");
                return StorageAuthorizationResult.SourceIPMismatch();
            }
        }

        var canonicalizedResource = BuildCanonicalizedResource(storageAccountName, absolutePath, sr);

        // The User Delegation SAS string-to-sign format evolved across service versions.
        // Service version is encoded in the sv= query parameter and determines which format to use.
        //
        //  sv >= 2026-04-06 (Azure.Storage.Blobs >= 12.28.0 — Dynamic User Delegation SAS):
        //    sp\nst\nse\nresource\nskoid\nsktid\nskt\nske\nsks\nskv\nsaoid\nsuoid\nscid\nskdtid\nsoid\nsip\nspr\nsv\nsr\n\n\n\n\nrscc\nrscd\nrsce\nrscl\nrsct
        //
        //  sv >= 2026-02-06 (Azure.Storage.Blobs 12.27.x — Principal-Bound User Delegation SAS):
        //    sp\nst\nse\nresource\nskoid\nsktid\nskt\nske\nsks\nskv\nsaoid\nsuoid\nscid\n(empty skdtid)\nsoid\nsip\nspr\nsv\nsr\n\n\nrscc\nrscd\nrsce\nrscl\nrsct
        //
        //  sv <  2026-02-06 (Azure.Storage.Blobs <= 12.26.x — legacy format):
        //    sp\nst\nse\nresource\nskoid\nsktid\nskt\nske\nsks\nskv\nsaoid\nsuoid\nscid\nsip\nspr\nsv\nsr\n\n\nrscc\nrscd\nrsce\nrscl\nrsct
        string stringToSign;
        if (string.Compare(sv, "2026-04-06", StringComparison.Ordinal) >= 0)
        {
            // 2026-04-06+ format: includes skdtid + soid and two extra empty slots for
            // canonicalizedSignedRequestHeaders / canonicalizedSignedRequestQueryParameters
            // (Dynamic User Delegation SAS, Azure.Storage.Blobs >= 12.28.0).
            stringToSign = string.Join("\n",
                sp, st, se, canonicalizedResource,
                skoid, sktid, skt, ske, sks, skv,
                saoid, suoid, scid,
                skdtid, soid,
                sip, spr, sv, sr,
                string.Empty, // signedSnapshotTime
                string.Empty, // signedEncryptionScope
                string.Empty, // canonicalizedSignedRequestHeaders
                string.Empty, // canonicalizedSignedRequestQueryParameters
                rscc, rscd, rsce, rscl, rsct);
        }
        else if (string.Compare(sv, "2026-02-06", StringComparison.Ordinal) >= 0)
        {
            // 2026-02-06 format: skdtid position exists but is always empty (placeholder added
            // as null in Azure.Storage.Blobs 12.27.x); soid may be populated for
            // Principal-Bound User Delegation SAS.  No request-headers/params slots yet.
            stringToSign = string.Join("\n",
                sp, st, se, canonicalizedResource,
                skoid, sktid, skt, ske, sks, skv,
                saoid, suoid, scid,
                string.Empty, // skdtid — always null/empty placeholder in this version
                soid,
                sip, spr, sv, sr,
                string.Empty, // signedSnapshotTime
                string.Empty, // signedEncryptionScope
                rscc, rscd, rsce, rscl, rsct);
        }
        else
        {
            // Legacy format (sv <= 2025-11-05, Azure.Storage.Blobs <= 12.26.x):
            // no skdtid / soid positions.
            stringToSign = string.Join("\n",
                sp, st, se, canonicalizedResource,
                skoid, sktid, skt, ske, sks, skv,
                saoid, suoid, scid,
                sip, spr, sv, sr,
                string.Empty, // signedSnapshotTime
                string.Empty, // signedEncryptionScope
                rscc, rscd, rsce, rscl, rsct);
        }

        logger.LogDebug(nameof(UserDelegationSasValidator), nameof(Validate),
            "sr='{0}' CanonicalizedResource='{1}' StringToSign='{2}'",
            sr, canonicalizedResource, stringToSign.Replace("\n", "\\n"));

        var accountResult = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (accountResult.Result != OperationResult.Success || accountResult.Resource == null)
        {
            logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
                "Storage account '{0}' not found during User Delegation SAS validation.", storageAccountName);
            return StorageAuthorizationResult.AuthenticationFailed();
        }

        // Reject SAS tokens whose signed key start time (skt) predates the account's revocation timestamp.
        if (accountResult.Resource.UserDelegationKeyRevocationTime.HasValue &&
            !string.IsNullOrEmpty(skt) &&
            DateTimeOffset.TryParse(skt, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var sktTime) &&
            sktTime < accountResult.Resource.UserDelegationKeyRevocationTime.Value)
        {
            logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
                "User Delegation SAS key start '{0}' predates revocation timestamp '{1}' for account '{2}'. Denying.",
                skt, accountResult.Resource.UserDelegationKeyRevocationTime.Value, storageAccountName);
            return StorageAuthorizationResult.AuthenticationFailed();
        }

        // Re-derive the user delegation key from each account key and check the signature.
        // Both keys are tried so that SAS tokens remain valid after a key regeneration on the
        // other key, matching the same two-key pattern used by ServiceSasValidator.
        var derivationInput = string.Join("\n", skoid, sktid, skt, ske, sks, skv);
        foreach (var accountKeyBase64 in new[] { accountResult.Resource.Keys[0].Value, accountResult.Resource.Keys[1].Value })
        {
            var accountKeyBytes = Convert.FromBase64String(accountKeyBase64);
            var userDelegationKeyBytes = UserDelegationKeyResponse.ComputeHmacSha256(accountKeyBytes, derivationInput);

            using var hmac = new HMACSHA256(userDelegationKeyBytes);
            var computedSig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            if (computedSig == sig) return StorageAuthorizationResult.Authorized();
        }

        logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
            "User Delegation SAS signature mismatch for account '{0}'.", storageAccountName);
        return StorageAuthorizationResult.AuthenticationFailed();
    }

    private static string BuildCanonicalizedResource(string accountName, string absolutePath, string sr)
    {
        var segments = absolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var firstSegment = segments.Length > 0 ? segments[0] : string.Empty;

        return sr.Equals("c", StringComparison.OrdinalIgnoreCase)
            ? $"/blob/{accountName}/{firstSegment.ToLowerInvariant()}"
            : $"/blob/{accountName}{absolutePath}";
    }
}
