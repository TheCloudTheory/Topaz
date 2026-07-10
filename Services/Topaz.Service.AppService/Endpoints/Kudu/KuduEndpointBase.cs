using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService.Endpoints.Kudu;

internal abstract class KuduEndpointBase(ITopazLogger logger) : IEndpointDefinition
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(logger);
    
    public abstract string[] Endpoints { get; }
    public string[] Permissions => [];
    public string? ProviderNamespace => null;
    public string? RequiredHostServiceLabel => null;

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultAppServiceKuduPort], Protocol.Https);
    
    public abstract void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options);
    
    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker)
    {
        var header = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(header))
        {
            logger.LogDebug(nameof(KuduEndpointBase), nameof(Authorize), "Authorization header is missing.");
            
            AddBasicAuthHeader(response);
            return (false, null);
        }

        var segments = header.Split(" ");
        var scheme = segments[0];
        if (scheme != "Basic")
        {
            logger.LogDebug(nameof(KuduEndpointBase), nameof(Authorize), "Authorization scheme for Kudu request is set to {0}. It's not supported by Topaz.", scheme);
            
            AddBasicAuthHeader(response);
            return (false, null);
        }
        
        var authValue = segments[1];
        var decoded = Convert.FromBase64String(authValue);
        var decodedValue = Encoding.UTF8.GetString(decoded);
        var userNameAndPassword = decodedValue.Split(':');
        var siteName = context.Request.Host.Host.Split('.')[0];
        
        var result = _controlPlane.ValidateUsernameAndPassword(siteName, userNameAndPassword[0], userNameAndPassword[1]);
        if(result.Result == OperationResult.Success)
        {
            return (true, null);
        }

        logger.LogDebug(nameof(KuduEndpointBase), nameof(Authorize), "Authorization failed for Kudu request. Reason: {0}", result.Reason ?? "Unknown");
        
        AddBasicAuthHeader(response);
        return (false, null);
    }

    private static void AddBasicAuthHeader(HttpResponseMessage response)
    {
        response.Headers.Add("WWW-Authenticate", "Basic realm=\"Kudu\"");
    }
}