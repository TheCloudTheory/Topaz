using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService.Endpoints.Sites;

internal sealed class GetWebAppStacksEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints => ["GET /providers/Microsoft.Web/webAppStacks"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GetWebAppStacksEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var result = _controlPlane.GetWebAppStacks();
            response.CreateJsonContentResponse(new RawJsonResponse(result.Resource!));
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    private sealed class RawJsonResponse(string json)
    {
        public override string ToString() => json;
    }
}
