using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.Service.Authorization;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Endpoints.DataPlane;

internal class EventGridDataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private static readonly object EventGridContextKey = new();
    
    private readonly AzureAuthorizationAdapter _authAdapter = new(eventPipeline, logger);
    
    protected readonly EventGridTopicControlPlane ControlPlane = EventGridTopicControlPlane.New(eventPipeline, logger);
    protected readonly EventGridDataPlane DataPlane = EventGridDataPlane.New(EventGridTopicControlPlane.New(eventPipeline, logger));
    
    public virtual string[] Endpoints => [];
    public string[] Permissions => [];
    public string ProviderNamespace => "Microsoft.EventGrid";
    public string RequiredHostServiceLabel => "eventgrid";
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public virtual void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        throw new NotImplementedException();
    }
    
    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker)
    {
        var eventGridName = context.Request.Host.Host.Split('.')[0];
        if (string.IsNullOrEmpty(eventGridName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return (false, null);
        }

        var eventGridOperation = ControlPlane.FindByName(eventGridName);
        if (eventGridOperation.Result == OperationResult.NotFound || eventGridOperation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return (false, null);
        }

        var topic = eventGridOperation.Resource;
        var subscriptionIdentifier = topic.GetSubscription();
        var resourceGroupIdentifier = topic.GetResourceGroup();

        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            return (false, null);
        }

        // Bearer tokens (Topaz CLI / Entra ID) bypass HMAC validation.
        // Note that HMAC validation will be bypassed if `DisableLocalAuth` is set to `true`
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && !topic.Properties.DisableLocalAuth!.Value &&
            !TryValidateHmac(authHeader, context, ControlPlane.ListKeys(subscriptionIdentifier, resourceGroupIdentifier, eventGridName).Resource, logger))
        {
            return (false, null);
        }

        // Perform Bearer token validation if applicable
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = JwtHelper.ValidateJwt(authHeader);
            if (token == null)
            {
                logger.LogDebug(nameof(EventGridDataPlaneEndpointBase), nameof(Authorize),
                    "Invalid or unrecognized JWT — denying access.");
                
                return (false, null);
            }
            
            // Global admin always passes.
            if (token.Subject == Globals.GlobalAdminId)
            {
                context.Items[EventGridContextKey] = new EventGridTopicContext(eventGridName, subscriptionIdentifier, resourceGroupIdentifier);
                return (true, null);
            }

            if (!_authAdapter.PrincipalHasPermissions(subscriptionIdentifier, token.Subject, Permissions))
            {
                return (false, null);
            }
        }

        context.Items[EventGridContextKey] = new EventGridTopicContext(eventGridName, subscriptionIdentifier, resourceGroupIdentifier);
        return (true, null);
    }
    
    private static bool TryValidateHmac(string authHeader, HttpContext context, EventGridSharedAccessKey[]? keys, ITopazLogger log)
    {
        if (keys == null) return false;

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

        var key = keys.FirstOrDefault(k =>
            string.Equals(k.KeyName, keyId, StringComparison.OrdinalIgnoreCase));
        if (key?.KeyValue == null)
        {
            log.LogDebug(nameof(EventGridDataPlaneEndpointBase), nameof(TryValidateHmac),
                "Key ID '{0}' not found in store. Available IDs: {1}", keyId, string.Join(", ", keys.Select(k => k.KeyName)));
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
        try { keyBytes = Convert.FromBase64String(key.KeyValue); }
        catch (Exception ex)
        {
            log.LogDebug(nameof(EventGridDataPlaneEndpointBase), nameof(TryValidateHmac),
                "Failed to base64-decode key secret: {0}", ex.Message);
            return false;
        }

        using var hmac = new HMACSHA256(keyBytes);
        var computed = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

        var match = string.Equals(computed, signature, StringComparison.Ordinal);
        log.LogDebug(nameof(EventGridDataPlaneEndpointBase), nameof(TryValidateHmac),
            "HMAC validation: method={0} path={1} signedHeaders={2} stringToSign={3} computed={4} received={5} match={6}",
            context.Request.Method, pathAndQuery, signedHeadersValue, stringToSign, computed, signature, match);

        return match;
    }
}