using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Topaz.Service.Shared;

public interface IEndpointDefinition
{
    public string[] Endpoints { get; }
    public string[] Permissions { get; }
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol { get; }

    /// <summary>
    /// The Azure provider namespace this endpoint belongs to (e.g. "Microsoft.KeyVault").
    /// When non-null the Router checks that the namespace is registered for the request's
    /// subscription before invoking <see cref="GetResponse"/> and returns
    /// MissingSubscriptionRegistration (409) if it is not.
    /// Leave null for infrastructure endpoints (Subscription, ResourceGroup, Deployment, etc.).
    /// </summary>
    public string? ProviderNamespace => null;

    /// <summary>
    /// A DNS label (or two-label segment) that must appear immediately after the first
    /// dot-separated label of the incoming <c>Host</c> header for this endpoint to be
    /// selected by the Router.
    /// <para>
    /// For single-label values (e.g. <c>"blob"</c>) the Router checks that the second DNS
    /// label matches exactly, disambiguating storage sub-service endpoints that share the
    /// same port:
    /// <c>{account}.<b>blob</b>.storage.topaz.local.dev</c>
    /// </para>
    /// <para>
    /// For two-label values (e.g. <c>"ods.opinsights"</c>) the Router checks that the host
    /// remainder starts with that prefix, supporting services whose subdomain structure spans
    /// two labels:
    /// <c>{workspaceId}.<b>ods.opinsights</b>.topaz.local.dev</c>
    /// </para>
    /// Leave <c>null</c> (the default) for endpoints whose hostname does not carry a
    /// service-discriminating label (ARM, Key Vault, ACR, etc.).
    /// </summary>
    public string? RequiredHostServiceLabel => null;

    /// <summary>
    /// Determines whether the incoming request is authorized to invoke this endpoint.
    /// The default implementation delegates to the Router's ARM RBAC adapter via
    /// Data-plane endpoints (Key Vault, Storage) override this method to perform their own
    /// authentication scheme (Bearer, SharedKey) inside <see cref="GetResponse"/> instead.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="response"></param>
    /// <param name="armAuthChecker">
    /// The ARM RBAC checker supplied by the Router. The default implementation calls
    /// <see cref="IArmAuthorizationChecker.IsAuthorized"/> with this endpoint's
    /// <see cref="Permissions"/> and the request's Authorization header.
    /// Data-plane overrides that handle auth themselves should ignore this parameter.
    /// </param>
    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker)
    {
        return armAuthChecker.IsAuthorized(
            Permissions,
            context.Request.Headers["Authorization"].ToString(),
            context.Request.Path.Value);
    }

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options);
}
