using System.Net;
using System.Text;
using System.Text.Json;
using Azure.ResourceManager.Resources.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

public sealed class ResourceManagerEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane = new(new SubscriptionResourceProvider(logger));
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane = new(new ResourceGroupResourceProvider(logger), logger);
    private readonly ArmTemplateParser _templateParser = new();
    private readonly ResourceManagerResourceProvider _resourceProvider = new(logger);
    
    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}"
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
        var payload = JsonSerializer.Deserialize<Deployment>(content, GlobalSettings.JsonOptions);
        if (payload == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        var templateProperty = JsonDocument.Parse(content).RootElement.GetProperty("properties").GetProperty("template");
        var rawTemplate = JsonSerializer.Serialize(templateProperty, GlobalSettings.JsonOptions);
        var template = _templateParser.Parse(rawTemplate);
        var deploymentResource = new Models.DeploymentResource(subscriptionIdentifier, resourceGroupIdentifier, deploymentName, resourceGroup.resource!.Location, new DeploymentResourceProperties()
        {
            CorrelationId = Guid.NewGuid().ToString(),
            Mode = payload.Properties!.Mode,
            TemplateHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(template)))
        });
        
        _resourceProvider.CreateOrUpdate(deploymentName, deploymentResource);
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(JsonSerializer.Serialize(deploymentResource, GlobalSettings.JsonOptions), Encoding.UTF8, "application/json");
    }
}