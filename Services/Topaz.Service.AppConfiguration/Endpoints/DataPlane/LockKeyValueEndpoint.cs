using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

internal sealed class LockKeyValueEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["PUT /locks/{key}"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var key = Uri.UnescapeDataString(context.Request.Path.Value!.Split('/').Last());
        var label = context.Request.Query["label"].ToString();

        var kv = ControlPlane.SetKvLock(ctx.Sub, ctx.Rg, ctx.StoreName, key,
            string.IsNullOrEmpty(label) ? null : label, locked: true);

        if (kv == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.CreateJsonContentResponse(kv);
        response.StatusCode = HttpStatusCode.OK;
    }
}
