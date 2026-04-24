using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Credentials;

internal sealed class RegenerateContainerRegistryCredentialEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/regenerateCredential"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/regenerateCredential/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupName = path.ExtractValueFromPath(4);
        var registryName = path.ExtractValueFromPath(8);

        var request = JsonSerializer.Deserialize<RegenerateCredentialRequest>(
            context.Request.Body, GlobalSettings.JsonOptions);

        if (request?.Name == null)
        {
            response.CreateErrorResponse("INVALID_REQUEST", "Request body must include a 'name' field (password or password2).", HttpStatusCode.BadRequest);
            return;
        }

        var operation = _controlPlane.RegenerateCredential(
            subscriptionIdentifier,
            ResourceGroupIdentifier.From(resourceGroupName),
            registryName!,
            request.Name);

        switch (operation.Result)
        {
            case OperationResult.NotFound:
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode, registryName!, resourceGroupName!);
                return;
            case OperationResult.Failed:
                response.CreateErrorResponse(operation.Code ?? "OPERATION_FAILED", operation.Reason ?? string.Empty, HttpStatusCode.BadRequest);
                return;
        }

        var props = operation.Resource!.Properties;
        var payload = JsonSerializer.Serialize(new
        {
            username = props.AdminUsername,
            passwords = new[]
            {
                new { name = "password",  value = props.AdminPassword },
                new { name = "password2", value = props.AdminPassword2 ?? props.AdminPassword }
            }
        }, GlobalSettings.JsonOptions);

        response.CreateJsonContentResponse(payload);
    }
}
