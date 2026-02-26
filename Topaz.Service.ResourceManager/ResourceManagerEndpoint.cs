using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerEndpoint(Pipeline eventPipeline, ITopazLogger logger, TemplateDeploymentOrchestrator deploymentOrchestrator) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane = new(eventPipeline, new SubscriptionResourceProvider(logger));

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        new(new ResourceGroupResourceProvider(logger),
            new SubscriptionControlPlane(eventPipeline, new SubscriptionResourceProvider(logger)), logger);
    private readonly ResourceManagerControlPlane _controlPlane = new(new ResourceManagerResourceProvider(logger), deploymentOrchestrator);
    
    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments",
        "GET /subscriptions/{subscriptionId}/providers/{providerName}",
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}/validate"
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var deploymentName = context.Request.Path.Value.ExtractValueFromPath(8);
            var segments = context.Request.Path.Value.Split('/');
            
            var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
            if (subscriptionOperation.Result == OperationResult.NotFound)
            {
                response.StatusCode =  HttpStatusCode.NotFound;
                response.Content = new StringContent(subscriptionOperation.ToString());
                
                return;
            }

            var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
            if (CanHandleRequestBasedOnAvailableData(segments, resourceGroupOperation))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.Content = new StringContent(subscriptionOperation.ToString());
                
                return;
            }

            switch (context.Request.Method)
            {
                case "PUT":
                    if (!string.IsNullOrWhiteSpace(deploymentName))
                    {
                        HandleCreateOrUpdateDeployment(response, subscriptionIdentifier, resourceGroupOperation.Resource!, deploymentName, context.Request.Body);
                    }
                    break;
                case "GET":
                    if (segments.Length == 5)
                    {
                        var providerName = context.Request.Path.Value.ExtractValueFromPath(4);
                        HandleGetResourceProviderData(response, subscriptionIdentifier, providerName!);
                        break;
                    }
                    
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
                            deploymentName, context.Request.Body);
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
    }

    private static bool CanHandleRequestBasedOnAvailableData(string[] segments, ControlPlaneOperationResult<ResourceGroupResource> resourceGroupOperation)
    {
        return segments.Length != 5 && (resourceGroupOperation.Result == OperationResult.NotFound || resourceGroupOperation.Resource == null);
    }

    private void HandleGetResourceProviderData(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, string providerName)
    {
        var data = new ResourceProviderDataResponse(providerName)
        {
            Id = $"/subscriptions/{subscriptionIdentifier}/providers/{providerName}",
        };
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(data.ToString());
    }

    private void HandleValidateDeployment(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, string deploymentName, Stream input)
    {
        logger.LogDebug(nameof(ResourceManagerEndpoint), nameof(HandleValidateDeployment),
            "Subscription `{0}`, resource group `{1}`, deployment name: `{2}`", subscriptionIdentifier,
            resourceGroupIdentifier, deploymentName);
        
        using var reader = new StreamReader(input);
        
        var content = reader.ReadToEnd();
        var result = _controlPlane.ValidateDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName, content);
        
        // TODO: Finish validating a deployment
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
        
        logger.LogDebug(nameof(ResourceManagerEndpoint), nameof(HandleCreateOrUpdateDeployment), "Attempting to deserialize into {0}: {1}", nameof(CreateDeploymentRequest), content);
        
        var request = JsonSerializer.Deserialize<CreateDeploymentRequest>(content, GlobalSettings.JsonOptions);
        if (request?.Properties == null || string.IsNullOrWhiteSpace(request.Properties.Mode))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.CreateOrUpdateDeployment(subscriptionIdentifier, resourceGroup.GetResourceGroup(),
            deploymentName, JsonSerializer.Serialize(request.Properties.Template), request.Properties.Parameters?.Parameters, resourceGroup.Location,
            request.Properties.Mode);
        
        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(JsonSerializer.Serialize(result.resource, GlobalSettings.JsonOptions), Encoding.UTF8, "application/json");
    }
}