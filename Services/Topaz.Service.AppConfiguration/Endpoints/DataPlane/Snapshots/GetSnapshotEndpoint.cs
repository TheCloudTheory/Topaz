using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane.Snapshots;

internal sealed class GetSnapshotEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["GET /snapshots/{name}"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var snapshotName = context.Request.Path.Value.ExtractValueFromPath(2);
        
        var operation = ControlPlane.GetSnapshot(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier, ctx.StoreName,
            snapshotName!);
        
        response.CreateJsonContentResponse(operation.Resource!.Properties);
        response.Headers.ETag = new EntityTagHeaderValue($"\"{operation.Resource!.Properties.Etag}\"");
        response.Content.Headers.LastModified = operation.Resource!.LastModified;
        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.microsoft.appconfig.snapshot+json");
        response.Headers.TryAddWithoutValidation("Sync-Token", operation.Resource!.SyncToken);
    }
}