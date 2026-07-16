using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane;

/// <summary>
/// Abstract base class for all Cosmos DB data-plane endpoints.
///
/// Handles:
/// <list type="bullet">
///   <item>Port assignment to <see cref="GlobalSettings.DefaultCosmosDbPort"/>.</item>
///   <item>Bypass of the Router's ARM RBAC check (auth is done here instead).</item>
///   <item>
///     Master-key HMAC-SHA256 authentication matching the Cosmos DB REST API contract.
///     The request is validated against both the primary and secondary master keys;
///     the request is authorized when either key produces a matching signature.
///   </item>
/// </list>
///
/// Subclasses must call <see cref="IsRequestAuthorized"/> at the top of
/// <see cref="IEndpointDefinition.GetResponse"/> and return immediately when it
/// returns <c>false</c> (the 401 response has already been written).
/// </summary>
internal abstract class CosmosDataPlaneEndpointBase(CosmosDbDataPlane dataPlane, Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private static readonly TimeSpan ReplayWindow = TimeSpan.FromMinutes(15);
    
    private readonly CosmosDbDataPlaneAuthorizationChecker _bearerChecker = new(eventPipeline, logger);

    public abstract string[] Endpoints { get; }
    public abstract string[] Permissions { get; }
    public abstract string? ProviderNamespace { get; }

    public string? RequiredHostServiceLabel => null;

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultCosmosDbPort], Protocol.Https);

    /// <summary>
    /// Cosmos DB data-plane endpoints manage their own master-key auth via
    /// <see cref="IsRequestAuthorized"/>. The Router's ARM RBAC check is bypassed.
    /// </summary>
    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker) => (true, null);

    public abstract void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options);

    /// <summary>
    /// Convenience overload: resolves the subscription and resource scope from the request host
    /// and path, then delegates to the full overload.
    /// </summary>
    protected bool IsRequestAuthorized(HttpContext context, HttpResponseMessage response)
    {
        var ctx = dataPlane.ResolveAccountContext(context);
        var sub = ctx?.Sub ?? SubscriptionIdentifier.Empty;
        var scope = ctx != null
            ? ComputeResourceScope(ctx, context.Request.Path.Value ?? string.Empty)
            : string.Empty;
        return IsRequestAuthorized(context, sub, Permissions, scope, response);
    }

    /// <summary>
    /// Validates the master-key HMAC-SHA256 Authorization header for the incoming request.
    /// Writes a 401 JSON response and returns <c>false</c> on any auth failure.
    /// </summary>
    protected bool IsRequestAuthorized(HttpContext context, SubscriptionIdentifier subscriptionIdentifier, string[] requiredPermissions, HttpResponseMessage response)
    {
        var ctx = dataPlane.ResolveAccountContext(context);
        var scope = ctx != null
            ? ComputeResourceScope(ctx, context.Request.Path.Value ?? string.Empty)
            : string.Empty;
        return IsRequestAuthorized(context, subscriptionIdentifier, requiredPermissions, scope, response);
    }

    private bool IsRequestAuthorized(HttpContext context, SubscriptionIdentifier subscriptionIdentifier, string[] requiredPermissions, string resourceScope, HttpResponseMessage response)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            WriteUnauthorized(response, "The request is missing the required Authorization header.");
            return false;
        }

        if (!TryParseCosmosAuth(authHeader, out var authParsed))
        {
            WriteUnauthorized(response, "The Authorization header is malformed or does not use master-key authentication.");
            return false;
        }

        // Parsing of the x-ms-date header is required only if the auth header type is master.
        // For Entra ID type, the header is absent.
        var dateHeader = context.Request.Headers["x-ms-date"].ToString();
        var isEntraIdAuth = authParsed.type == "aad";
        if (!isEntraIdAuth && (string.IsNullOrEmpty(dateHeader) ||
                  !TryParseHttpDate(dateHeader, out var requestDate) ||
                  Math.Abs((DateTimeOffset.UtcNow - requestDate).TotalMinutes) >
                  ReplayWindow.TotalMinutes))
        {
            WriteUnauthorized(response,
                "The x-ms-date header is missing, unparseable, or outside the 15-minute replay-attack window.");
            return false;
        }

        var (resourceType, resourceLink) = ParseResourceTypeAndLink(context.Request.Path.Value ?? string.Empty);

        var method = context.Request.Method.ToLowerInvariant();
        var stringToSign = $"{method}\n{resourceType.ToLowerInvariant()}\n{resourceLink.ToLowerInvariant()}\n{dateHeader.ToLowerInvariant()}\n\n";
        var payload = Encoding.UTF8.GetBytes(stringToSign);
        var account = dataPlane.ResolveAccount(context);
        if (account?.Properties == null)
        {
            WriteUnauthorized(response, "The Cosmos DB account could not be resolved from the request host.");
            return false;
        }

        if (account.Properties.DisableLocalAuth && !isEntraIdAuth)
        {
            logger.LogDebug(nameof(CosmosDataPlaneEndpointBase), nameof(IsRequestAuthorized),
                "Local authentication is disabled for account '{0}'", account.Name);
            
            WriteUnauthorized(response, "Local authentication is disabled for this Cosmos DB account.");
            return false;
        }

        if(isEntraIdAuth)
        {
            logger.LogDebug(nameof(CosmosDataPlaneEndpointBase), nameof(IsRequestAuthorized),
                "Checking bearer authentication for '{0}'", account.Name);

            if (_bearerChecker.IsAuthorizedForBearerWithScope(requiredPermissions, authParsed.sig, resourceScope))
                return true;

            WriteUnauthorized(response, "The request is not authorized to perform this operation.");
            return false;
        }
        
        if (TryVerifySignature(account.Properties.PrimaryMasterKey, payload, authParsed.sig, logger))
            return true;

        if (TryVerifySignature(account.Properties.SecondaryMasterKey, payload, authParsed.sig, logger))
            return true;

        logger.LogDebug(nameof(CosmosDataPlaneEndpointBase), nameof(IsRequestAuthorized),
            "HMAC signature mismatch for account '{0}'", account.Name);

        WriteUnauthorized(response, "The input authorization token cannot serve the request. Please check that the expected payload is built as per the protocol, and check the key being used to sign the payload.");
        return false;
    }

    /// <summary>
    /// Parses the Cosmos DB Authorization header format:
    /// <c>type=master&amp;ver=1.0&amp;sig=&lt;base64&gt;</c>
    /// Returns <c>false</c> when the type is not <c>master</c> or sig is absent.
    /// </summary>
    /// <summary>
    /// Computes the ARM resource scope for the Cosmos DB resource being accessed.
    /// Examples:
    ///   /             → .../databaseAccounts/{account}
    ///   /dbs/{db}     → .../databaseAccounts/{account}/sqlDatabases/{db}
    ///   /dbs/{db}/colls/{coll}/... → .../databaseAccounts/{account}/sqlDatabases/{db}/containers/{coll}
    /// </summary>
    private static string ComputeResourceScope(CosmosDbAccountContext ctx, string path)
    {
        var accountScope =
            $"/subscriptions/{ctx.Sub.Value}/resourceGroups/{ctx.Rg.Value}" +
            $"/providers/Microsoft.DocumentDB/databaseAccounts/{ctx.Account.Name}";

        var segments = path.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        // segments[0]="dbs", segments[1]=dbName, segments[2]="colls", segments[3]=collName
        if (segments.Length >= 2 &&
            string.Equals(segments[0], "dbs", StringComparison.OrdinalIgnoreCase))
        {
            var dbScope = $"{accountScope}/sqlDatabases/{segments[1]}";
            if (segments.Length >= 4 &&
                string.Equals(segments[2], "colls", StringComparison.OrdinalIgnoreCase))
                return $"{dbScope}/containers/{segments[3]}";
            return dbScope;
        }

        return accountScope;
    }

    private bool TryParseCosmosAuth(string header, out (string sig, string type) result)
    {
        var sig = string.Empty;
        // The SDK URL-encodes the header value; decode it first.
        var decoded = Uri.UnescapeDataString(header);
        var parts = decoded.Split('&');
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            var eq = part.IndexOf('=');
            if (eq > 0)
                dict[part[..eq]] = part[(eq + 1)..];
        }

        // Note that if Entra ID token is used for authorization, 
        // the header will be "type=aad&amp;ver=1.0&amp;sig=ey..."
        if (dict.TryGetValue("type", out var type) &&
            (string.Equals(type, "master", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(type, "aad", StringComparison.OrdinalIgnoreCase)))
        {
            var sigResult= dict.TryGetValue("sig", out sig!) && !string.IsNullOrEmpty(sig);
            if(sigResult)
            {
                result = (sig, type);
                return true;
            }
            
            result = (sig, type);
            return false;
        }

        logger.LogDebug(nameof(CosmosDataPlaneEndpointBase), nameof(TryParseCosmosAuth), $"Invalid Cosmos auth type: {type}");
        result = (string.Empty, string.Empty);
        return false;

    }

    private static bool TryParseHttpDate(string value, out DateTimeOffset result)
    {
        // Cosmos DB SDK sends RFC 1123 format (e.g. "Tue, 17 Jun 2026 12:34:56 GMT")
        return DateTimeOffset.TryParseExact(value,
            ["ddd, dd MMM yyyy HH:mm:ss 'GMT'", "ddd, d MMM yyyy HH:mm:ss 'GMT'"],
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out result);
    }

    /// <summary>
    /// Derives the (resourceType, resourceLink) pair from a Cosmos DB REST request path.
    ///
    /// Cosmos DB REST paths alternate between type segments and id segments:
    /// <c>/dbs/{db}/colls/{coll}/docs/{doc}</c>
    ///
    /// Rules (after stripping the leading '/'):
    /// <list type="bullet">
    ///   <item>Odd segment count → resource type = last segment; link = path minus last segment.</item>
    ///   <item>Even segment count → resource type = second-to-last segment; link = full trimmed path.</item>
    /// </list>
    ///
    /// Examples:
    /// <code>
    ///   /dbs              → type="dbs",  link=""
    ///   /dbs/mydb         → type="dbs",  link="dbs/mydb"
    ///   /dbs/mydb/colls   → type="colls",link="dbs/mydb"
    ///   /dbs/mydb/colls/c → type="colls",link="dbs/mydb/colls/c"
    /// </code>
    /// </summary>
    internal static (string resourceType, string resourceLink) ParseResourceTypeAndLink(string path)
    {
        var trimmed = path.TrimStart('/');
        if (string.IsNullOrEmpty(trimmed))
            return (string.Empty, string.Empty);

        var segments = trimmed.Split('/');

        if (segments.Length % 2 == 1)
        {
            // Odd: last segment is the resource type; link is everything before it
            var type = segments[^1];
            var link = segments.Length > 1
                ? string.Join('/', segments[..^1])
                : string.Empty;
            return (type, link);
        }
        else
        {
            // Even: second-to-last segment is the resource type; link is the full path
            var type = segments[^2];
            return (type, trimmed);
        }
    }

    private static bool TryVerifySignature(string? base64Key, byte[] payload, string expectedSig, ITopazLogger logger)
    {
        if (string.IsNullOrEmpty(base64Key))
            return false;

        try
        {
            var keyBytes = Convert.FromBase64String(base64Key);
            using var hmac = new HMACSHA256(keyBytes);
            var computed = hmac.ComputeHash(payload);
            var computedBase64 = Encoding.UTF8.GetBytes(Convert.ToBase64String(computed));
            var expectedBytes = Encoding.UTF8.GetBytes(expectedSig);

            // Pad to equal length before constant-time compare to avoid length leaks
            if (computedBase64.Length != expectedBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(computedBase64, expectedBytes);
        }
        catch (Exception ex)
        {
            logger.LogDebug(nameof(CosmosDataPlaneEndpointBase), nameof(TryVerifySignature),
                "Failed to verify HMAC signature: {0}", ex.Message);
            return false;
        }
    }

    private static void WriteUnauthorized(HttpResponseMessage response, string message)
    {
        response.StatusCode = HttpStatusCode.Unauthorized;
        var body = new CosmosUnauthorizedError { Code = "Unauthorized", Message = message };
        response.CreateJsonContentResponse(body, HttpStatusCode.Unauthorized);
    }

    private sealed class CosmosUnauthorizedError
    {
        [JsonPropertyName("code")]
        public string Code { get; init; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        public override string ToString() =>
            System.Text.Json.JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
