using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.VirtualNetwork.Models.Requests;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class VirtualNetworkServiceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}",
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}"
    ];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    private readonly VirtualNetworkControlPlane _controlPlane = new(new VirtualNetworkResourceProvider(logger), logger);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query,
        GlobalOptions options, Guid correlationId)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();
        
        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
            var virtualNetworkName = path.ExtractValueFromPath(8);
            
            switch (method)
            {
                case "PUT":
                    HandleCreateOrUpdateVirtualNetworkRequest(response, input, subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
                    break;
                case "GET":
                    HandleGetVirtualNetworkRequest(response, subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
                    break;
                case "POST":
                    break;
                case "DELETE":
                    break;
                default:
                    response.StatusCode = HttpStatusCode.NotFound;
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
        
        return response;
    }

    private void HandleCreateOrUpdateVirtualNetworkRequest(HttpResponseMessage response, Stream input,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string? virtualNetworkName)
    {
        if (string.IsNullOrWhiteSpace(virtualNetworkName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateVirtualNetworkRequest>(content, GlobalSettings.JsonOptions)!;
        var operation = _controlPlane.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier,
            virtualNetworkName, request);

        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = operation.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource!.ToString());
    }

    private void HandleGetVirtualNetworkRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string? virtualNetworkName)
    {
        if (string.IsNullOrWhiteSpace(virtualNetworkName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var operation = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, virtualNetworkName);
        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource?.ToString()!);
    }
}