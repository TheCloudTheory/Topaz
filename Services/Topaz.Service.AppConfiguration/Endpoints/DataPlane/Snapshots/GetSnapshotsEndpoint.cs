using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.AppConfiguration.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane.Snapshots;

internal sealed class GetSnapshotsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["GET /snapshots"];
    public override string[] Permissions => ["Microsoft.AppConfiguration/configurationStores/snapshots/read"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        context.Request.QueryString.TryGetValueForKey("status", out var status);
        context.Request.QueryString.TryGetValueForKey("name", out var name);
        
        var operation = ControlPlane.GetSnapshots(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier, ctx.StoreName, status, name);
        
        response.CreateJsonContentResponse(SnapshotListResultResponse.From(operation.Resource!));
        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.microsoft.appconfig.snapshot+json");
    }
}