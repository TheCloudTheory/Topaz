using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Endpoints.DataPlane;

internal sealed class PublishEventGridEventEndpoint(Pipeline eventPipeline, ITopazLogger logger) : EventGridDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["POST /api/events"];
    
    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        
        DataPlane.PublishEventGridEvent();
    }
}