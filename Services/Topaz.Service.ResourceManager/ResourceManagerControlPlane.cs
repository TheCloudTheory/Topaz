using Azure.Deployments.Core.Entities;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Topaz.ResourceManager;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.EventHub;
using Topaz.Service.KeyVault;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.ServiceBus;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage;
using Topaz.Service.VirtualNetwork;
using Topaz.Shared;
using DeploymentResource = Topaz.Service.ResourceManager.Models.DeploymentResource;

namespace Topaz.Service.ResourceManager;

internal sealed class ResourceManagerControlPlane(
    ResourceManagerResourceProvider provider,
    TemplateDeploymentOrchestrator templateDeploymentOrchestrator,
    ITopazLogger logger)
{
    private const string DeploymentNotFoundMessageTemplate = "Deployment {0} not found";
    private const string DeploymentNotFoundCode = "DeploymentNotFound";

    private readonly ArmTemplateEngineFacade _templateEngineFacade = new();

    public (OperationResult result, DeploymentResource resource) CreateOrUpdateDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName, string content,
        Dictionary<string, CreateDeploymentRequest.ParameterValue>? parameters, string location,
        string deploymentMode)
    {
        var template = _templateEngineFacade.Parse(content);
        var deploymentResource = new DeploymentResource(subscriptionIdentifier, resourceGroupIdentifier, deploymentName,
            location, DeploymentResourceProperties.New(deploymentMode, content, parameters));

        provider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier, deploymentName, deploymentResource);

        var subscriptionMetadata = new SubscriptionMetadata(subscriptionIdentifier);
        var resourceGroupMetadata =
            new ResourceGroupMetadata(subscriptionIdentifier, resourceGroupIdentifier, location);
        var metadata = new DeploymentMetadata
        {
            { DeploymentMetadata.SubscriptionKey, JToken.Parse(subscriptionMetadata.ToString()) },
            { DeploymentMetadata.ResourceGroupKey,  JToken.Parse(resourceGroupMetadata.ToString())}
        };

        var metadataInsensitive =
            metadata.ToInsensitiveDictionary(meta => meta.Key, meta => meta.Value);

        templateDeploymentOrchestrator.EnqueueTemplateDeployment(subscriptionIdentifier, resourceGroupIdentifier,
            template, deploymentResource, metadataInsensitive);

        return (OperationResult.Success, deploymentResource);
    }

    public ControlPlaneOperationResult<DeploymentResource> GetDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName)
    {
        var resource =
            provider.GetAs<DeploymentResource>(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (resource == null ||
            !resource.IsInSubscription(subscriptionIdentifier) ||
            !resource.IsInResourceGroup(resourceGroupIdentifier))
        {
            return new ControlPlaneOperationResult<DeploymentResource>(OperationResult.NotFound, null,
                string.Format(DeploymentNotFoundMessageTemplate, deploymentName), DeploymentNotFoundCode);
        }

        return new ControlPlaneOperationResult<DeploymentResource>(OperationResult.Success, resource, null, null);
    }

    public OperationResult DeleteDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName)
    {
        var resource =
            provider.GetAs<DeploymentResource>(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (resource == null ||
            !resource.IsInSubscription(subscriptionIdentifier) ||
            !resource.IsInResourceGroup(resourceGroupIdentifier))
        {
            return OperationResult.NotFound;
        }

        provider.Delete(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        return OperationResult.Deleted;
    }

    public (OperationResult result, DeploymentResource[] resource) GetDeployments(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var resources = provider.ListAs<DeploymentResource>(subscriptionIdentifier, resourceGroupIdentifier, null, 8);

        var filteredBySubscriptionAndResourceGroup = resources.Where(deployment =>
                deployment.IsInSubscription(subscriptionIdentifier) &&
                deployment.IsInResourceGroup(resourceGroupIdentifier))
            .ToArray();

        return (OperationResult.Success, filteredBySubscriptionAndResourceGroup);
    }

    public ControlPlaneOperationResult<DeploymentValidateResult> ValidateDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName, CreateDeploymentRequest request)
    {
        try
        {
            var template = request.ToTemplate();
            // ValidateTemplate throws on invalid input; returns void on success
            _templateEngineFacade.Validate(template);

            return new ControlPlaneOperationResult<DeploymentValidateResult>(OperationResult.Success,
                DeploymentValidateResult.FromRequest(subscriptionIdentifier, resourceGroupIdentifier, deploymentName, request),
                null, null);
        }
        catch (Exception ex)
        {
            logger.LogDebug(nameof(ResourceManagerControlPlane), nameof(ValidateDeployment),
                "Template validation failed: {0}", ex.Message);

            return new ControlPlaneOperationResult<DeploymentValidateResult>(OperationResult.Failed,
                null, ex.Message, "InvalidTemplate");
        }
    }

    public ExportTemplateResult ExportTemplate(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        ExportTemplateRequest request)
    {
        logger.LogDebug(nameof(ResourceManagerControlPlane), nameof(ExportTemplate),
            "Executing {0}: {1}/{2}", nameof(ExportTemplate), subscriptionIdentifier, resourceGroupIdentifier);

        var options = ParseExportOptions(request.Options);
        var resources = CollectResourcesFromGroup(subscriptionIdentifier, resourceGroupIdentifier);

        // Wildcard "*" or null/empty means export all; otherwise filter to the requested resource IDs.
        var requestedIds = request.Resources is { Length: > 0 } r && !(r.Length == 1 && r[0] == "*")
            ? new HashSet<string>(r, StringComparer.OrdinalIgnoreCase)
            : null;

        if (requestedIds != null)
            resources = resources.Where(res => requestedIds.Contains(res.Id));

        var parameters = new Dictionary<string, JsonNode?>();
        var templateResources = new JsonArray();

        foreach (var resource in resources)
        {
            var templateResource = BuildTemplateResource(resource, options, parameters);
            if (templateResource != null)
                templateResources.Add(templateResource);
        }

        var template = new JsonObject
        {
            ["$schema"] = JsonValue.Create("https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"),
            ["contentVersion"] = JsonValue.Create("1.0.0.0"),
            ["parameters"] = JsonNode.Parse(JsonSerializer.Serialize(parameters, GlobalSettings.JsonOptions)),
            ["resources"] = templateResources
        };

        return new ExportTemplateResult { Template = template };
    }

    private IEnumerable<GenericResource> CollectResourcesFromGroup(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        const uint depth = 8;
        var results = new List<GenericResource>();

        results.AddRange(new KeyVaultResourceProvider(logger).ListAs<GenericResource>(subscriptionIdentifier, resourceGroupIdentifier, null, depth));
        results.AddRange(new ContainerRegistryResourceProvider(logger).ListAs<GenericResource>(subscriptionIdentifier, resourceGroupIdentifier, null, depth));
        results.AddRange(new ManagedIdentityResourceProvider(logger).ListAs<GenericResource>(subscriptionIdentifier, resourceGroupIdentifier, null, depth));
        results.AddRange(new ServiceBusResourceProvider(logger).ListAs<GenericResource>(subscriptionIdentifier, resourceGroupIdentifier, null, depth));
        results.AddRange(new EventHubResourceProvider(logger).ListAs<GenericResource>(subscriptionIdentifier, resourceGroupIdentifier, null, depth));
        results.AddRange(new StorageResourceProvider(logger).ListAs<GenericResource>(subscriptionIdentifier, resourceGroupIdentifier, null, depth));
        results.AddRange(new VirtualNetworkResourceProvider(logger).ListAs<GenericResource>(subscriptionIdentifier, resourceGroupIdentifier, null, depth));

        return results;
    }

    private static readonly Dictionary<string, string> ApiVersions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft.KeyVault/vaults"] = "2022-07-01",
        ["Microsoft.ContainerRegistry/registries"] = "2023-07-01",
        ["Microsoft.Storage/storageAccounts"] = "2023-01-01",
        ["Microsoft.EventHub/namespaces"] = "2024-01-01",
        ["Microsoft.ServiceBus/namespaces"] = "2022-10-01-preview",
        ["Microsoft.Network/virtualNetworks"] = "2023-11-01",
        ["Microsoft.ManagedIdentity/userAssignedIdentities"] = "2023-01-31",
    };

    private static readonly HashSet<string> ExcludedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.ResourceGroups/group",
        "Microsoft.Resources/deployments",
    };

    private static JsonNode? BuildTemplateResource(
        GenericResource resource,
        ExportTemplateOptions options,
        Dictionary<string, JsonNode?> parameters)
    {
        if (ExcludedTypes.Contains(resource.Type))
            return null;

        if (!ApiVersions.TryGetValue(resource.Type, out var apiVersion))
            return null;

        var resourceObj = new JsonObject
        {
            ["type"] = resource.Type,
            ["apiVersion"] = apiVersion,
        };

        if (options.SkipAllParameterization || options.SkipResourceNameParameterization)
        {
            resourceObj["name"] = resource.Name;
        }
        else
        {
            var paramName = GenerateUniqueParamName(resource.Name, "Name", parameters);
            resourceObj["name"] = $"[parameters('{paramName}')]";
            parameters[paramName] = BuildParameter(
                "string",
                options.IncludeParameterDefaultValue ? resource.Name : null,
                options.IncludeComments ? $"Name of the {resource.Type} resource." : null);
        }

        if (options.SkipAllParameterization)
        {
            resourceObj["location"] = resource.Location;
        }
        else
        {
            const string locationParamName = "location";
            if (!parameters.ContainsKey(locationParamName))
            {
                parameters[locationParamName] = BuildParameter(
                    "string",
                    options.IncludeParameterDefaultValue ? resource.Location : null,
                    options.IncludeComments ? "Azure region for all resources." : null);
            }
            resourceObj["location"] = $"[parameters('{locationParamName}')]";
        }

        if (resource.Tags != null)
            resourceObj["tags"] = JsonNode.Parse(JsonSerializer.Serialize(resource.Tags, GlobalSettings.JsonOptions));

        if (resource.Sku != null)
            resourceObj["sku"] = JsonNode.Parse(JsonSerializer.Serialize(resource.Sku, GlobalSettings.JsonOptions));

        if (!string.IsNullOrEmpty(resource.Kind))
            resourceObj["kind"] = resource.Kind;

        if (resource.Properties != null)
            resourceObj["properties"] = JsonNode.Parse(JsonSerializer.Serialize(resource.Properties, GlobalSettings.JsonOptions));

        return resourceObj;
    }

    private static JsonNode BuildParameter(string type, string? defaultValue, string? description)
    {
        var param = new JsonObject { ["type"] = type };
        if (defaultValue != null)
            param["defaultValue"] = defaultValue;
        if (description != null)
            param["metadata"] = new JsonObject { ["description"] = description };
        return param;
    }

    private static string GenerateUniqueParamName(string resourceName, string suffix, Dictionary<string, JsonNode?> existingParams)
    {
        var baseName = ToCamelCase(resourceName) + suffix;
        if (!existingParams.ContainsKey(baseName))
            return baseName;

        var counter = 1;
        while (existingParams.ContainsKey($"{baseName}{counter}"))
            counter++;
        return $"{baseName}{counter}";
    }

    private static string ToCamelCase(string name)
    {
        var sanitized = new string(name.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrEmpty(sanitized)) return "resource";
        return char.ToLowerInvariant(sanitized[0]) + sanitized[1..];
    }

    private static ExportTemplateOptions ParseExportOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
            return new ExportTemplateOptions();

        var parts = options.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new ExportTemplateOptions(
            IncludeParameterDefaultValue: parts.Contains("IncludeParameterDefaultValue", StringComparer.OrdinalIgnoreCase),
            IncludeComments: parts.Contains("IncludeComments", StringComparer.OrdinalIgnoreCase),
            SkipResourceNameParameterization: parts.Contains("SkipResourceNameParameterization", StringComparer.OrdinalIgnoreCase),
            SkipAllParameterization: parts.Contains("SkipAllParameterization", StringComparer.OrdinalIgnoreCase)
        );
    }

    private sealed record ExportTemplateOptions(
        bool IncludeParameterDefaultValue = false,
        bool IncludeComments = false,
        bool SkipResourceNameParameterization = false,
        bool SkipAllParameterization = false);
}