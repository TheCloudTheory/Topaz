using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService.Endpoints.Kudu;

internal sealed class PostZipDeployEndpoint(ITopazLogger logger)
    : KuduEndpointBase(logger)
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(logger);
    private readonly ITopazLogger _logger = logger;

    public override string[] Endpoints => ["POST /api/zipdeploy"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        _logger.LogDebug(nameof(PostZipDeployEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

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
            _logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
