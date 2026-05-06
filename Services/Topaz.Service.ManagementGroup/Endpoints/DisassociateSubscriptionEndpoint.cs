using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ManagementGroup.Endpoints;

internal sealed class DisassociateSubscriptionEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ManagementGroupControlPlane _controlPlane = ManagementGroupControlPlane.New(logger);

    public string[] Endpoints =>
        ["DELETE /providers/Microsoft.Management/managementGroups/{groupId}/subscriptions/{subscriptionId}"];

    public string[] Permissions => ["Microsoft.Management/managementGroups/subscriptions/delete"];

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

        var operation = _controlPlane.DisassociateSubscription(groupId, subscriptionId);

        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.NotFound);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = null;
    }
}
