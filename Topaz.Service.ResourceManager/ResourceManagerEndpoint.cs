using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane = new(new SubscriptionResourceProvider(logger));
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane = new(new ResourceGroupResourceProvider(logger), logger);
    private readonly ResourceManagerControlPlane _controlPlane = new(new ResourceManagerResourceProvider(logger));
    
    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}"
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
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
            var deploymentName = path.ExtractValueFromPath(8);

            switch (method)
            {
                case "PUT":
                    if (!string.IsNullOrWhiteSpace(deploymentName))
                    {
                        HandleCreateOrUpdateDeployment(response, subscriptionIdentifier, resourceGroupIdentifier, deploymentName, input);
                    }
                    break;
                case "GET":
                    if (!string.IsNullOrWhiteSpace(deploymentName))
                    {
                        HandleGetDeployment(response, subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
                    }
                    break;
                default:
                    response.StatusCode = HttpStatusCode.NotFound;
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, ex.Message);
        }
        
        return response;
    }

    private void HandleGetDeployment(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string deploymentName)
    {
        var subscription = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.result == OperationResult.NotFound)
        {
            response.StatusCode =  HttpStatusCode.NotFound;
            return;
        }

        var resourceGroup = _resourceGroupControlPlane.Get(resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var deployment = _controlPlane.GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (deployment.result == OperationResult.NotFound || deployment.resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(deployment.resource.ToString());
    }

    private void HandleCreateOrUpdateDeployment(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName, Stream input)
    {
        var subscription = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscription.result == OperationResult.NotFound)
        {
            response.StatusCode =  HttpStatusCode.NotFound;
            return;
        }

        var resourceGroup = _resourceGroupControlPlane.Get(resourceGroupIdentifier);
        if (resourceGroup.result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        using var reader = new StreamReader(input);
        
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateDeploymentRequest>(content, GlobalSettings.JsonOptions);
        if (request?.Properties == null || string.IsNullOrWhiteSpace(request.Properties.Mode))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        var result = _controlPlane.CreateOrUpdateDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName, content, resourceGroup.resource!.Location, request.Properties.Mode);
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(JsonSerializer.Serialize(result.resource, GlobalSettings.JsonOptions), Encoding.UTF8, "application/json");
    }
}