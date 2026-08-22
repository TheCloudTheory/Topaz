using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane.Snapshots;

internal sealed class CheckSnapshotsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["HEAD /snapshots"];
    public override string[] Permissions => ["Microsoft.AppConfiguration/configurationStores/snapshots/read"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/vnd.microsoft.appconfig.snapshot+json");
        response.Headers.TryAddWithoutValidation("Sync-Token", "topaz=MA==;sn=1");
    }
}