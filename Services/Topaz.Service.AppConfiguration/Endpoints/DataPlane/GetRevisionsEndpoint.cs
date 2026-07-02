using System.Net;
using System.Text;
using System.Text.Json;
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
            string.IsNullOrEmpty(keyFilter) || keyFilter == "\0" ? null : keyFilter,
            string.IsNullOrEmpty(labelFilter) || labelFilter == "\0" ? null : labelFilter);

        response.Content = new StringContent(JsonSerializer.Serialize(new { items = kvs }, GlobalSettings.JsonOptions), Encoding.UTF8, "application/json");
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"{Guid.NewGuid():N}\"");
        response.StatusCode = HttpStatusCode.OK;
    }
}
