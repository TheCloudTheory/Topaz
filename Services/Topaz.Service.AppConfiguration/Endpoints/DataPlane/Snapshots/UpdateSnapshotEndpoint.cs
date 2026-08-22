using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.AppConfiguration.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane.Snapshots;

internal sealed class UpdateSnapshotEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["PATCH /snapshots/{name}"];
    public override string[] Permissions => ["Microsoft.AppConfiguration/configurationStores/snapshots/write"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var snapshotName = context.Request.Path.Value.ExtractValueFromPath(2);
        var ifMatch = context.Request.Headers["If-Match"].ToString();
        var ifNoneMatch = context.Request.Headers["If-None-Match"].ToString();

        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<UpdateSnapshotRequest>(reader.ReadToEnd(), GlobalSettings.JsonOptions);

        var operation = ControlPlane.UpdateSnapshot(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier, ctx.StoreName,
            snapshotName!, request!, ifMatch, ifNoneMatch);

        switch (operation.Result)
        {
            case OperationResult.BadRequest:
                response.CreateBadRequestResponse(operation);
                return;
            case OperationResult.PreconditionFailed:
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                return;
        }

        if (operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(operation);
            return;
        }

        response.CreateJsonContentResponse(operation.Resource!.Properties);
        response.Headers.ETag = new EntityTagHeaderValue($"\"{operation.Resource!.Properties.Etag}\"");
        response.Content.Headers.LastModified = operation.Resource!.LastModified;
        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.microsoft.appconfig.snapshot+json");
        response.Headers.TryAddWithoutValidation("Sync-Token", operation.Resource!.SyncToken);
    }
}