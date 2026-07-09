using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.LogAnalytics.Endpoints;

internal sealed class GetWorkspaceEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly LogAnalyticsServiceControlPlane _controlPlane =
        LogAnalyticsServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.OperationalInsights";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.OperationalInsights/workspaces/{workspaceName}"
    ];

    public string[] Permissions => ["Microsoft.OperationalInsights/workspaces/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);

        var result = _controlPlane.Get(sub, rg, name!);
        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!, HttpStatusCode.NotFound);
            return;
        }

        response.CreateJsonContentResponse(result.Resource);
    }
}
