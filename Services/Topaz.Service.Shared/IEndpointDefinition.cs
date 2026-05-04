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

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options);
}
