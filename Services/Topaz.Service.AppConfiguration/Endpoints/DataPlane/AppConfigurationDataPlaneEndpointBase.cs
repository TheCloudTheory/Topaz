using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Topaz.EventPipeline;
using Topaz.Service.AppConfiguration.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

/// <summary>
/// Base class for all App Configuration data-plane endpoints.
/// Resolves the store from the <c>{storeName}.azconfig.topaz.local.dev</c> Host header
/// and validates the HMAC-SHA256 Authorization scheme used by the Azure SDK.
/// On success, stores the resolved context in <see cref="HttpContext.Items"/> under
/// <see cref="StoreContextKey"/> for use by <c>GetResponse</c>.
/// </summary>
internal abstract class AppConfigurationDataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    protected static readonly object StoreContextKey = new();

    protected readonly AppConfigurationServiceControlPlane ControlPlane =
        AppConfigurationServiceControlPlane.New(eventPipeline, logger);

    public abstract string[] Endpoints { get; }
    public string[] Permissions => [];
    public string? ProviderNamespace => null;
    public string? RequiredHostServiceLabel => null;

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultAppConfigurationPort], Protocol.Https);

    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker)
    {
        var storeName = context.Request.Host.Host.Split('.')[0];
        if (string.IsNullOrEmpty(storeName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return (false, null);
        }

        var storeOp = ControlPlane.FindByName(storeName);
        if (storeOp.Result == OperationResult.NotFound || storeOp.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return (false, null);
        }

        var store = storeOp.Resource;
        var sub = store.GetSubscription();
        var rg = store.GetResourceGroup();

        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return (false, null);
        }

        // Bearer tokens (Topaz CLI / Entra ID) bypass HMAC validation.
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) &&
            !TryValidateHmac(authHeader, context, ControlPlane.GetAccessKeys(sub, rg, storeName), logger))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return (false, null);
        }

        context.Items[StoreContextKey] = new AppConfigurationStoreContext(storeName, sub, rg);
        return (true, null);
    }

    public abstract void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options);

    protected static AppConfigurationStoreContext GetStoreContext(HttpContext context) =>
        (AppConfigurationStoreContext)context.Items[StoreContextKey]!;

    private static bool TryValidateHmac(string authHeader, HttpContext context, AppConfigurationAccessKeyStore? keyStore, ITopazLogger log)
    {
        if (keyStore == null) return false;

        // HMAC-SHA256 Credential={keyId}&SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature={base64}
        const string prefix = "HMAC-SHA256 ";
        if (!authHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        var parts = authHeader[prefix.Length..]
            .Split('&')
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1], StringComparer.OrdinalIgnoreCase);

        if (!parts.TryGetValue("Credential", out var keyId) ||
            !parts.TryGetValue("SignedHeaders", out var signedHeadersValue) ||
            !parts.TryGetValue("Signature", out var signature))
            return false;

        var key = keyStore.Keys.FirstOrDefault(k =>
            string.Equals(k.Id, keyId, StringComparison.OrdinalIgnoreCase));
        if (key?.Value == null)
        {
            log.LogDebug(nameof(AppConfigurationDataPlaneEndpointBase), nameof(TryValidateHmac),
                "Key ID '{0}' not found in store. Available IDs: {1}", keyId, string.Join(", ", keyStore.Keys.Select(k => k.Id)));
            return false;
        }

        // Build the signed header values in the order declared by SignedHeaders.
        var signedHeaders = signedHeadersValue.Split(';');
        var headerValues = signedHeaders.Select(name => name.Equals("host", StringComparison.OrdinalIgnoreCase)
            ? context.Request.Host.Value
            : context.Request.Headers[name].ToString()).ToArray();

        // Use the raw (percent-encoded) request target so it matches what the SDK signed.
        var pathAndQuery = context.Features.Get<IHttpRequestFeature>()?.RawTarget
            ?? (context.Request.Path.Value + context.Request.QueryString.Value);
        var stringToSign = $"{context.Request.Method}\n{pathAndQuery}\n{string.Join(';', headerValues)}";

        byte[] keyBytes;
        try { keyBytes = Convert.FromBase64String(key.Value); }
        catch (Exception ex)
        {
            log.LogDebug(nameof(AppConfigurationDataPlaneEndpointBase), nameof(TryValidateHmac),
                "Failed to base64-decode key secret: {0}", ex.Message);
            return false;
        }

        using var hmac = new HMACSHA256(keyBytes);
        var computed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        var match = string.Equals(computed, signature, StringComparison.Ordinal);
        log.LogDebug(nameof(AppConfigurationDataPlaneEndpointBase), nameof(TryValidateHmac),
            "HMAC validation: method={0} path={1} signedHeaders={2} stringToSign={3} computed={4} received={5} match={6}",
            context.Request.Method, pathAndQuery, signedHeadersValue, stringToSign, computed, signature, match);

        return match;
    }
}
