using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Endpoints.DataPlane;

internal sealed class PublishCloudEventEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : EventGridDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["POST /"];
    
    public override string[] Permissions => ["Microsoft.EventGrid/events/send/action"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var isBatch = context.Request.Headers.ContainsKey("application/cloudevents-batch+json");
        var ctx = GetEventGridContext(context);

        using var reader = new StreamReader(context.Request.Body);
        var data = isBatch ?
            JsonSerializer.Deserialize<EventGridCloudEventSchema[]>(reader.ReadToEnd(), GlobalSettings.JsonOptions)
            : [JsonSerializer.Deserialize<EventGridCloudEventSchema>(reader.ReadToEnd(), GlobalSettings.JsonOptions)!];

        var result = DataPlane.PublishCloudEvent(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier,
            ctx.TopicName, data!);
        if (result.Result != OperationResult.Success)
        {
            response.CreateErrorResponse(result);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
    }
}