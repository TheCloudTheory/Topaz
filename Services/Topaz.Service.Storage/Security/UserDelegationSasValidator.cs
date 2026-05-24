using System.Globalization;
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
/// IP range enforcement (sip=) is not implemented and is treated as a known limitation.
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
    /// Validates a User Delegation SAS token. Returns true when the HMAC-SHA256 signature is
    /// valid and the token has not expired.
    /// </summary>
    public bool Validate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName,
        string absolutePath,
        QueryString query)
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
        var saoid = parsed["saoid"] ?? string.Empty;
        var suoid = parsed["suoid"] ?? string.Empty;
        var scid  = parsed["scid"]  ?? string.Empty;

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
                return false;
            }

            if (DateTimeOffset.UtcNow >= expiry)
            {
                logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
                    "User Delegation SAS has expired (se={0}, now={1}). Denying.", se, DateTimeOffset.UtcNow);
                return false;
            }
        }

        // IP restriction — known limitation: sip= is not enforced.
        if (!string.IsNullOrEmpty(sip))
        {
            logger.LogDebug(nameof(UserDelegationSasValidator), nameof(Validate),
                "User Delegation SAS contains sip='{0}' restriction — IP enforcement is not implemented; passing.", sip);
        }

        var canonicalizedResource = BuildCanonicalizedResource(storageAccountName, absolutePath, sr);

        // User Delegation SAS StringToSign (API version 2020-12-06+):
        // sp\nst\nse\ncanonicalized_resource\nskoid\nsktid\nskt\nske\nsks\nskv\nsaoid\nsuoid\nscid\nsip\nspr\nsv\nsr\n\n\nrscc\nrscd\nrsce\nrscl\nrsct
        var stringToSign = string.Join("\n",
            sp, st, se, canonicalizedResource,
            skoid, sktid, skt, ske, sks, skv,
            saoid, suoid, scid,
            sip, spr, sv, sr,
            string.Empty, // signedSnapshotTime
            string.Empty, // signedEncryptionScope
            rscc, rscd, rsce, rscl, rsct);

        logger.LogDebug(nameof(UserDelegationSasValidator), nameof(Validate),
            "sr='{0}' CanonicalizedResource='{1}' StringToSign='{2}'",
            sr, canonicalizedResource, stringToSign.Replace("\n", "\\n"));

        var accountResult = controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (accountResult.Result != OperationResult.Success || accountResult.Resource == null)
        {
            logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
                "Storage account '{0}' not found during User Delegation SAS validation.", storageAccountName);
            return false;
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

            if (computedSig == sig) return true;
        }

        logger.LogError(nameof(UserDelegationSasValidator), nameof(Validate),
            "User Delegation SAS signature mismatch for account '{0}'.", storageAccountName);
        return false;
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
