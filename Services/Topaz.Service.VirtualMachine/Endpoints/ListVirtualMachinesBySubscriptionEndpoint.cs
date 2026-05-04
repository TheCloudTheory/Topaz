using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualMachine.Endpoints;

internal sealed class ListVirtualMachinesBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly VirtualMachineServiceControlPlane _controlPlane =
        VirtualMachineServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Compute/virtualMachines"
    ];

    public string[] Permissions => ["Microsoft.Compute/virtualMachines/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListVirtualMachinesBySubscriptionEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        var subscriptionIdentifier =
            SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var vms = _controlPlane.ListBySubscription(subscriptionIdentifier);
        if (vms.Result != OperationResult.Success || vms.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionResourcesResponse
        {
            Value = vms.Resource
                .Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!)
                .ToArray()
        };

        response.CreateJsonContentResponse(result);
    }
}
