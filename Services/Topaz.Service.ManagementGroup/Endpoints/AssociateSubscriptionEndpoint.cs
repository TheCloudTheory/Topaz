using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class AssociateSubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);
    private readonly SubscriptionControlPlane _subControlPlane = SubscriptionControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
        ["PUT /providers/Microsoft.Management/managementGroups/{groupId}/subscriptions/{subscriptionId}"];

    public string[] Permissions => ["Microsoft.Management/managementGroups/subscriptions/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var groupId = context.Request.Path.Value.ExtractValueFromPath(4);
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(6);

        if (string.IsNullOrWhiteSpace(groupId) || string.IsNullOrWhiteSpace(subscriptionId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var displayName = ResolveSubscriptionDisplayName(subscriptionId);
        var operation = _controlPlane.AssociateSubscription(groupId, subscriptionId, displayName);

        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.NotFound);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.CreateJsonContentResponse(operation.Resource!);
    }

    private string? ResolveSubscriptionDisplayName(string subscriptionId)
    {
        var result = _subControlPlane.Get(SubscriptionIdentifier.From(subscriptionId));
        return result.Result == OperationResult.Success ? result.Resource?.DisplayName : null;
    }
}
