using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualMachine.Models.Requests;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualMachine.Endpoints;

internal sealed class CreateOrUpdateVirtualMachineEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly VirtualMachineServiceControlPlane _controlPlane =
        VirtualMachineServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Compute/virtualMachines/{virtualMachineName}"
    ];

    public string[] Permissions => ["Microsoft.Compute/virtualMachines/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateOrUpdateVirtualMachineEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier =
                SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier =
                ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var virtualMachineName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(virtualMachineName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            logger.LogDebug(nameof(CreateOrUpdateVirtualMachineEndpoint), nameof(GetResponse),
                "Processing payload: {0}", content);

            var request = JsonSerializer.Deserialize<CreateOrUpdateVirtualMachineRequest>(
                content, GlobalSettings.JsonOptions);
            if (request == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = _controlPlane.CreateOrUpdate(
                subscriptionIdentifier, resourceGroupIdentifier, virtualMachineName, request);

            if ((result.Result != OperationResult.Created && result.Result != OperationResult.Updated)
                || result.Resource == null)
            {
                response.CreateErrorResponse(result.Code!, result.Reason!);
                return;
            }

            var statusCode = result.Result == OperationResult.Created
                ? HttpStatusCode.Created
                : HttpStatusCode.OK;
            response.CreateJsonContentResponse(result.Resource, statusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
