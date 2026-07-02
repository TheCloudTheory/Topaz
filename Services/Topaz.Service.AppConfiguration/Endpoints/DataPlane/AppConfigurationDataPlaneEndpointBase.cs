using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
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

        if (!TryValidateHmac(authHeader, context, ControlPlane.GetAccessKeys(sub, rg, storeName)))
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

    private static bool TryValidateHmac(string authHeader, HttpContext context, AppConfigurationAccessKeyStore? keyStore)
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
            !parts.TryGetValue("Signature", out var signature))
            return false;

        var key = keyStore.Keys.FirstOrDefault(k =>
            string.Equals(k.Id, keyId, StringComparison.OrdinalIgnoreCase));
        if (key?.Value == null) return false;

        var date = context.Request.Headers["x-ms-date"].ToString();
        var host = context.Request.Host.Value;
        var contentHash = context.Request.Headers["x-ms-content-sha256"].ToString();
        var pathAndQuery = context.Request.Path.Value + context.Request.QueryString.Value;

        var stringToSign = $"{context.Request.Method}\n{pathAndQuery}\n{date};{host};{contentHash}";

        byte[] keyBytes;
        try { keyBytes = Convert.FromBase64String(key.Value); }
        catch { return false; }

        using var hmac = new HMACSHA256(keyBytes);
        var computed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        return string.Equals(computed, signature, StringComparison.Ordinal);
    }
}
