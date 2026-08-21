using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane.Kv;

internal sealed class ListLabelsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["GET /labels"];
    public override string[] Permissions => ["Microsoft.AppConfiguration/configurationStores/keyValuePairs/read"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var kvs = ControlPlane.ListKvs(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier, ctx.StoreName, null, null, null);

        if (kvs.Result == OperationResult.NotFound)
        {
            response.CreateNotFoundResponse(kvs);
            return;
        }
        
        if (kvs.Result != OperationResult.Success)
        {
            response.CreateErrorResponse(kvs);
            return;
        }

        var labels = kvs.Resource!
            .Select(kv => kv.Label)
            .Distinct()
            .Select(l => new { name = l })
            .ToArray();

        response.Content = new StringContent(JsonSerializer.Serialize(new { items = labels }, GlobalSettings.JsonOptions), Encoding.UTF8, "application/json");
        response.StatusCode = HttpStatusCode.OK;
    }
}
