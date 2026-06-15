using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService.Endpoints.Kudu;

internal sealed class PostZipDeployEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => null;

    public string[] Endpoints => ["POST /api/zipdeploy"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultAppServiceKuduPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(PostZipDeployEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var siteName = context.Request.Host.Host.Split('.')[0];

            var found = _controlPlane.FindSiteByName(siteName);
            if (found == null)
            {
                response.CreateErrorResponse("SiteNotFound", $"Site '{siteName}' not found.", HttpStatusCode.NotFound);
                return;
            }

            var id = _controlPlane.ZipDeploy(found.Value.Sub, found.Value.Rg, siteName, context.Request.Body);

            response.StatusCode = HttpStatusCode.Accepted;
            response.Headers.Location = new Uri($"/api/deployments/{id}", UriKind.Relative);
            response.Content = new StringContent(string.Empty);
            response.Content.Headers.ContentType =
                System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
