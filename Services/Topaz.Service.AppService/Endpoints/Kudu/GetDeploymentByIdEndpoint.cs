using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppService.Endpoints.Kudu;

internal sealed class GetDeploymentByIdEndpoint(ITopazLogger logger)
    : KuduEndpointBase(logger)
{
    private readonly AppServiceSiteControlPlane _controlPlane = AppServiceSiteControlPlane.New(logger);
    private readonly ITopazLogger _logger = logger;

    public override string[] Endpoints => ["GET /api/deployments/{id}"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        _logger.LogDebug(nameof(GetDeploymentByIdEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var siteName = context.Request.Host.Host.Split('.')[0];
            var id = context.Request.Path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries)[2];

            var found = _controlPlane.FindSiteByName(siteName);
            if (found == null)
            {
                response.CreateErrorResponse("SiteNotFound", $"Site '{siteName}' not found.", HttpStatusCode.NotFound);
                return;
            }

            var record = _controlPlane.GetDeployment(found.Value.Sub, found.Value.Rg, siteName, id);
            if (record == null)
            {
                response.CreateErrorResponse("DeploymentNotFound", $"Deployment '{id}' not found.", HttpStatusCode.NotFound);
                return;
            }

            response.CreateJsonContentResponse(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
