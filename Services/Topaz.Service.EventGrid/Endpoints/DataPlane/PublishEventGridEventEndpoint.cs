using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Endpoints.DataPlane;

internal sealed class PublishEventGridEventEndpoint(Pipeline eventPipeline, ITopazLogger logger) : EventGridDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["POST /api/events"];
    
    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetEventGridContext(context);
        
        using var reader = new StreamReader(context.Request.Body);
        var data =
            JsonSerializer.Deserialize<EventGridEventSchema[]>(reader.ReadToEnd(), GlobalSettings.JsonOptions);
        
        var result = DataPlane.PublishEventGridEvent(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier, ctx.TopicName, data!);
        if (result.Result != OperationResult.Success)
        {
            response.CreateErrorResponse(result);
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
    }
}