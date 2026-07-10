using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService.Endpoints.Kudu;

internal sealed class GetDeploymentsEndpoint(ITopazLogger logger)
    : KuduEndpointBase(logger)
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(logger);
    private readonly ITopazLogger _logger = logger;

    public override string[] Endpoints => ["GET /api/deployments"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        _logger.LogDebug(nameof(GetDeploymentsEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var siteName = context.Request.Host.Host.Split('.')[0];

            var found = _controlPlane.FindSiteByName(siteName);
            if (found == null)
            {
                response.CreateErrorResponse("SiteNotFound", $"Site '{siteName}' not found.", HttpStatusCode.NotFound);
                return;
            }

            var records = _controlPlane.ListDeployments(found.Value.Sub, found.Value.Rg, siteName);
            var json = JsonSerializer.Serialize(records, GlobalSettings.JsonOptions);
            response.Content = new StringContent(json);
            response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
