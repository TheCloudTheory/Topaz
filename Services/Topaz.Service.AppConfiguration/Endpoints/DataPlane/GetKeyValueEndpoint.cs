using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

internal sealed class GetKeyValueEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["GET /kv/{key}"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var key = Uri.UnescapeDataString(context.Request.Path.Value!.Split('/').Last());
        var label = context.Request.Query["label"].ToString();

        var kv = ControlPlane.GetKv(ctx.Sub, ctx.Rg, ctx.StoreName, key,
            string.IsNullOrEmpty(label) ? null : label);

        if (kv == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var ifNoneMatch = context.Request.Headers["If-None-Match"].ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, $"\"{kv.Etag}\"", StringComparison.Ordinal))
        {
            response.StatusCode = HttpStatusCode.NotModified;
            return;
        }

        response.CreateJsonContentResponse(kv);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"{kv.Etag}\"");
        response.Content.Headers.LastModified = kv.LastModified;
        response.StatusCode = HttpStatusCode.OK;
    }
}
