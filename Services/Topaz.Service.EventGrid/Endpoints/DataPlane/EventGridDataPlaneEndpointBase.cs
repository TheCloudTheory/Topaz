using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventGrid.Endpoints.DataPlane;

internal class EventGridDataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    protected readonly EventGridDataPlane DataPlane = EventGridDataPlane.New(EventGridTopicControlPlane.New(eventPipeline, logger));
    
    public virtual string[] Endpoints => [];
    public string[] Permissions => [];
    public string ProviderNamespace => "Microsoft.EventGrid";
    public string RequiredHostServiceLabel => "eventgrid";
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public virtual void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        throw new NotImplementedException();
    }
}