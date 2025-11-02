using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerEndpoint(ITopazLogger logger, TemplateDeploymentOrchestrator deploymentOrchestrator) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane = new(new SubscriptionResourceProvider(logger));
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane = new(new ResourceGroupResourceProvider(logger), new SubscriptionControlPlane(new SubscriptionResourceProvider(logger)), logger);
    private readonly ResourceManagerControlPlane _controlPlane = new(new ResourceManagerResourceProvider(logger), deploymentOrchestrator);
    
    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments",
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}/validate"
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
            
            var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
            if (subscriptionOperation.Result == OperationResult.NotFound)
            {
                response.StatusCode =  HttpStatusCode.NotFound;
                response.Content = new StringContent(subscriptionOperation.ToString());
                
                return response;
            }

            var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
            if (resourceGroupOperation.Result == OperationResult.NotFound || resourceGroupOperation.Resource == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(subscriptionOperation.ToString());
                
                return response;
            }

            switch (method)
            {
                case "PUT":
                    if (!string.IsNullOrWhiteSpace(deploymentName))
                    {
                        HandleCreateOrUpdateDeployment(response, subscriptionIdentifier, resourceGroupOperation.Resource, deploymentName, input);
                    }
                    break;
                case "GET":
                    if (!string.IsNullOrWhiteSpace(deploymentName))
                    {
                        HandleGetDeployment(response, subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
                    }
                    else
                    {
                        HandleGetDeployments(response, subscriptionIdentifier, resourceGroupIdentifier);
                    }
                    break;
                case "DELETE":
                    if (!string.IsNullOrWhiteSpace(deploymentName))
                    {
                        HandleDeleteDeployment(response, subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
                    }
                    break;
                case "POST":
                    if (!string.IsNullOrWhiteSpace(deploymentName))
                    {
                        HandleValidateDeployment(response, subscriptionIdentifier, resourceGroupIdentifier,
                            deploymentName, input);
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

    private void HandleValidateDeployment(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string deploymentName, Stream input)
    {
        logger.LogDebug(nameof(ResourceManagerEndpoint), nameof(HandleValidateDeployment), subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        
        using var reader = new StreamReader(input);
        
        var content = reader.ReadToEnd();
        var result = _controlPlane.ValidateDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName, content);
    }

    private void HandleGetDeployments(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var result = _controlPlane.GetDeployments(subscriptionIdentifier, resourceGroupIdentifier);
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(new DeploymentListResult(result.resource!).ToString());
    }

    private void HandleDeleteDeployment(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string deploymentName)
    {
        var deploymentOperation = _controlPlane.GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (deploymentOperation.Result == OperationResult.NotFound || deploymentOperation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var result = _controlPlane.DeleteDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (result != OperationResult.Deleted)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        response.StatusCode = HttpStatusCode.NoContent;
    }

    private void HandleGetDeployment(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string deploymentName)
    {
        var deploymentOperation = _controlPlane.GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (deploymentOperation.Result == OperationResult.NotFound || deploymentOperation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(deploymentOperation.Resource.ToString());
    }

    private void HandleCreateOrUpdateDeployment(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupResource resourceGroup,
        string deploymentName, Stream input)
    {
        using var reader = new StreamReader(input);
        
        var content = reader.ReadToEnd();
        
        logger.LogDebug($"Attempting to deserialize into {nameof(CreateDeploymentRequest)}: {content}");
        
        var request = JsonSerializer.Deserialize<CreateDeploymentRequest>(content, GlobalSettings.JsonOptions);
        if (request?.Properties == null || string.IsNullOrWhiteSpace(request.Properties.Mode))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.CreateOrUpdateDeployment(subscriptionIdentifier, resourceGroup.GetResourceGroup(),
            deploymentName, JsonSerializer.Serialize(request.Properties.Template), resourceGroup.Location,
            request.Properties.Mode);
        
        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(JsonSerializer.Serialize(result.resource, GlobalSettings.JsonOptions), Encoding.UTF8, "application/json");
    }
}