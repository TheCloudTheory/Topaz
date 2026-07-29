using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualMachine.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualMachine.Endpoints.AvailabilitySets;

internal sealed class ListAvalabilitySetsBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AvailabilitySetControlPlane _controlPlane =
        AvailabilitySetControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Compute/availabilitySets"
    ];

    public string[] Permissions => ["Microsoft.Compute/availabilitySets/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateOrUpdateVirtualMachineEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));
        
        var subscriptionIdentifier =
            SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

        var result = _controlPlane.ListBySubscription(subscriptionIdentifier);

        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.CreateJsonContentResponse(AvailabilitySetListResultResponse.From(result.Resource));
    }
}