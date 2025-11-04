using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork;

internal sealed class VirtualNetworkServiceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Network/virtualNetworks/{virtualNetworkName}"   
    ];
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();
        
        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupSegment = path.ExtractValueFromPath(4);
            var virtualNetworkName = path.ExtractValueFromPath(8);
            
            switch (method)
            {
                case "PUT":
                    break;
                case "GET":
                    HandleGetVirtualNetworkRequest(response, subscriptionIdentifier, resourceGroupSegment, virtualNetworkName);
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

    private void HandleGetVirtualNetworkRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, string? resourceGroupSegment, string? virtualNetworkName)
    {
        if (string.IsNullOrWhiteSpace(virtualNetworkName))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        
    }
}