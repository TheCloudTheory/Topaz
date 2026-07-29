using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualMachine.Endpoints.AvailabilitySets;

internal sealed class DeleteAvailabilitySetEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AvailabilitySetControlPlane _controlPlane =
        AvailabilitySetControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/availabilitySets/{availabilitySetName}"
    ];

    public string[] Permissions => ["Microsoft.Compute/availabilitySets/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateOrUpdateVirtualMachineEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));
        
        var subscriptionIdentifier =
            SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier =
            ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var availabilitySetName = context.Request.Path.Value.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(availabilitySetName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.Delete(
            subscriptionIdentifier, resourceGroupIdentifier, availabilitySetName);

        if (result.Result != OperationResult.Deleted)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
    }
}