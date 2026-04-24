using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Credentials;

internal sealed class ListContainerRegistryCredentialsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/listCredentials"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/listCredentials/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupName = path.ExtractValueFromPath(4);
        var registryName = path.ExtractValueFromPath(8);

        var operation = _controlPlane.Get(
            subscriptionIdentifier,
            ResourceGroupIdentifier.From(resourceGroupName),
            registryName!);

        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode, registryName!, resourceGroupName!);
            return;
        }

        var props = operation.Resource!.Properties;

        if (!props.AdminUserEnabled)
        {
            response.CreateErrorResponse("ADMIN_USER_DISABLED", $"Admin user is disabled for registry '{registryName}'.", HttpStatusCode.BadRequest);
            return;
        }

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
