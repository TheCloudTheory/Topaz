using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

/// <summary>Returns current key-values as revision history (no change tracking in Topaz).</summary>
internal sealed class GetRevisionsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["GET /revisions"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var keyFilter = context.Request.Query["key"].ToString();
        var labelFilter = context.Request.Query["label"].ToString();

        var kvs = ControlPlane.ListKvs(ctx.Sub, ctx.Rg, ctx.StoreName,
            string.IsNullOrEmpty(keyFilter) ? null : keyFilter,
            string.IsNullOrEmpty(labelFilter) ? null : labelFilter);

        response.CreateJsonContentResponse(new { items = kvs });
        response.StatusCode = HttpStatusCode.OK;
    }
}
