using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Endpoints.DataPlane;

internal class EventGridDataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    protected readonly EventGridTopicControlPlane EventGridTopicControlPlane = EventGridTopicControlPlane.New(eventPipeline, logger);
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

        var eventGridOperation = EventGridTopicControlPlane.FindByName(eventGridName);
        if (eventGridOperation.Result == OperationResult.NotFound || eventGridOperation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return (false, null);
        }

        var store = eventGridOperation.Resource;
        var subscriptionIdentifier = store.GetSubscription();
        var resourceGroupIdentifier = store.GetResourceGroup();

        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            return (false, null);
        }

        // Bearer tokens (Topaz CLI / Entra ID) bypass HMAC validation.
        // Note that HMAC validation will be bypassed if `DisableLocalAuth` is set to `true`
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && !store.Properties.DisableLocalAuth!.Value &&
            !TryValidateHmac(authHeader, context, ControlPlane.GetAccessKeys(subscriptionIdentifier, resourceGroupIdentifier, eventGridName), logger))
        {
            return (false, null);
        }

        // Perform Bearer token validation if applicable
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = JwtHelper.ValidateJwt(authHeader);
            if (token == null)
            {
                logger.LogDebug(nameof(AppConfigurationDataPlaneEndpointBase), nameof(Authorize),
                    "Invalid or unrecognized JWT — denying access.");
                
                return (false, null);
            }
            
            // Global admin always passes.
            if (token.Subject == Globals.GlobalAdminId)
            {
                context.Items[StoreContextKey] = new AppConfigurationStoreContext(eventGridName, subscriptionIdentifier, resourceGroupIdentifier);
                return (true, null);
            }

            if (!_authAdapter.PrincipalHasPermissions(subscriptionIdentifier, token.Subject, Permissions))
            {
                return (false, null);
            }
        }

        context.Items[StoreContextKey] = new AppConfigurationStoreContext(eventGridName, subscriptionIdentifier, resourceGroupIdentifier);
        return (true, null);
    }
}