using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

internal sealed class ListLabelsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["GET /labels"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var kvs = ControlPlane.ListKvs(ctx.Sub, ctx.Rg, ctx.StoreName, null, null);

        var labels = kvs
            .Select(kv => kv.Label)
            .Distinct()
            .Select(l => new { name = l })
            .ToArray();

        response.CreateJsonContentResponse(new { items = labels });
        response.StatusCode = HttpStatusCode.OK;
    }
}
