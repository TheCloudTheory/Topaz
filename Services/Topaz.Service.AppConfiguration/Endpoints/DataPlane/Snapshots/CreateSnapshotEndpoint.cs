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

internal sealed class CreateSnapshotEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["PUT /snapshots/{name}"];
    public override string[] Permissions => ["Microsoft.AppConfiguration/configurationStores/snapshots/write"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var snapshotName = context.Request.Path.Value.ExtractValueFromPath(2);

        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<CreateSnapshotRequest>(reader.ReadToEnd(), GlobalSettings.JsonOptions);

        var operation = ControlPlane.CreateSnapshot(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier, ctx.StoreName,
            snapshotName!, request!);

        if (operation.Result != OperationResult.Created)
        {
            response.CreateErrorResponse(operation);
            return;
        }

        response.CreateJsonContentResponse(operation.Resource!.Properties, HttpStatusCode.Created);
        response.Headers.ETag = new EntityTagHeaderValue($"\"{operation.Resource!.Properties.Etag}\"");
        response.Content.Headers.LastModified = operation.Resource!.LastModified;
        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.microsoft.appconfig.snapshot+json");
        response.Headers.TryAddWithoutValidation("Sync-Token", operation.Resource!.SyncToken);
    }
}