using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

internal sealed class ListKeyValuesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["GET /kv"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var keyFilter = context.Request.Query["key"].ToString();
        var labelFilter = context.Request.Query["label"].ToString();

        // Treat \0 (null byte) as "no filter" — Azure CLI uses this as the "no label" sentinel.
        var kvs = ControlPlane.ListKvs(ctx.Sub, ctx.Rg, ctx.StoreName,
            string.IsNullOrEmpty(keyFilter) || keyFilter == "\0" ? null : keyFilter,
            string.IsNullOrEmpty(labelFilter) || labelFilter == "\0" ? null : labelFilter);

        response.Content = new StringContent(JsonSerializer.Serialize(new { items = kvs }, GlobalSettings.JsonOptions), Encoding.UTF8, "application/json");
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"{Guid.NewGuid():N}\"");
        response.StatusCode = HttpStatusCode.OK;
    }
}
