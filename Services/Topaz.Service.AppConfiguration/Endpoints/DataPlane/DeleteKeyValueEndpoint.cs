using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

internal sealed class DeleteKeyValueEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["DELETE /kv/{key}"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var key = Uri.UnescapeDataString(context.Request.Path.Value!.Split('/').Last());
        var label = context.Request.Query["label"].ToString();
        var labelOrNull = string.IsNullOrEmpty(label) ? null : label;

        var deleted = ControlPlane.DeleteKv(ctx.Sub, ctx.Rg, ctx.StoreName, key, labelOrNull);
        if (deleted == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.CreateJsonContentResponse(deleted);
        response.StatusCode = HttpStatusCode.OK;
    }
}
