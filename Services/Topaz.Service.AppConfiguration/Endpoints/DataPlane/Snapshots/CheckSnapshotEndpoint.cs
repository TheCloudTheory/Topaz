using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane.Snapshots;

internal sealed class CheckSnapshotEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["HEAD /snapshots/{name}"];
    public override string[] Permissions => ["Microsoft.AppConfiguration/configurationStores/snapshots/read"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var snapshotName = context.Request.Path.Value.ExtractValueFromPath(2);
        var ifMatch = context.Request.Headers["If-Match"].ToString();
        var ifNoneMatch = context.Request.Headers["If-None-Match"].ToString();
        
        var operation = ControlPlane.GetSnapshot(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier, ctx.StoreName,
            snapshotName!, ifMatch, ifNoneMatch);
        
        if(operation.Result == OperationResult.PreconditionFailed)
        {
            response.StatusCode = HttpStatusCode.PreconditionFailed;
            return;
        }
        
        response.Content = new ByteArrayContent([]);
        response.Headers.ETag = new EntityTagHeaderValue($"\"{operation.Resource!.Properties.Etag}\"");
        response.Content.Headers.LastModified = operation.Resource!.LastModified;
        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.microsoft.appconfig.snapshot+json");
        response.Headers.TryAddWithoutValidation("Sync-Token", operation.Resource!.SyncToken);
    }
}