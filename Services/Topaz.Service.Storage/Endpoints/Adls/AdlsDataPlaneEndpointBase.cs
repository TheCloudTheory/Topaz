using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Adls;

internal class AdlsDataPlaneEndpointBase(Pipeline eventPipeline, ITopazLogger logger) : DataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    public string ProviderNamespace => "Microsoft.Storage";
    public string RequiredHostServiceLabel => "dfs";
    public virtual string[] Endpoints => [];
    public virtual string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultStoragePort], Protocol.Https);
    public virtual void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        throw new NotImplementedException();
    }
}