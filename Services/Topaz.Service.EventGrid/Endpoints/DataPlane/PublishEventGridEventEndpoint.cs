using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Endpoints.DataPlane;

internal sealed class PublishEventGridEventEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : EventGridDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["POST /api/events"];
    
    public override string[] Permissions => ["Microsoft.EventGrid/events/send/action"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var eventSchemaTypeHeader = context.Request.Headers["Content-Type"];
        var ctx = GetEventGridContext(context);

        using var reader = new StreamReader(context.Request.Body);
        var json = reader.ReadToEnd();

        var result = DataPlane.PublishEvent(ctx.SubscriptionIdentifier, ctx.ResourceGroupIdentifier,
            ctx.TopicName, json, eventSchemaTypeHeader.ToString());
        if (result.Result == OperationResult.BadRequest)
        {
            response.CreateErrorResponse(result, HttpStatusCode.BadRequest);
            return;
        }
        
        if (result.Result != OperationResult.Success)
        {
            response.CreateErrorResponse(result);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
    }
}