using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using Azure.ResourceManager.Resources.Models;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json.Linq;
using System.Text;
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
using DeploymentMetadata = Topaz.Service.ResourceManager.Deployment.DeploymentMetadata;
using WhatIfOperationResult = Topaz.Service.ResourceManager.Models.Responses.WhatIfOperationResult;
using WhatIfChange = Topaz.Service.ResourceManager.Models.Responses.WhatIfChange;

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

    public OperationResult CancelDeployment(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName)
    {
        var deploymentOp = GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (deploymentOp.Result == OperationResult.NotFound)
            return OperationResult.NotFound;

        // Only Created (queued-but-not-started) deployments can be cancelled
        var provisioningState = deploymentOp.Resource!.Properties.ProvisioningState;
        if (provisioningState != ResourcesProvisioningState.Created.ToString())
            return OperationResult.Conflict;

        return templateDeploymentOrchestrator.CancelDeployment(
            $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/Microsoft.Resources/deployments/{deploymentName}");
    }

    public ControlPlaneOperationResult<ExportTemplateResult> ExportDeploymentTemplate(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName)
    {
        var deploymentOp = GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName);
        if (deploymentOp.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<ExportTemplateResult>(OperationResult.NotFound, null,
                string.Format(DeploymentNotFoundMessageTemplate, deploymentName), DeploymentNotFoundCode);

        var templateBase64 = deploymentOp.Resource!.Properties.TemplateHash;
        if (string.IsNullOrEmpty(templateBase64))
            return new ControlPlaneOperationResult<ExportTemplateResult>(OperationResult.Failed, null,
                $"Template is not available for deployment '{deploymentName}'.", "TemplateNotAvailable");

        try
        {
            var templateJson = Encoding.UTF8.GetString(Convert.FromBase64String(templateBase64));
            var template = JsonNode.Parse(templateJson)!;
            return new ControlPlaneOperationResult<ExportTemplateResult>(OperationResult.Success,
                new ExportTemplateResult { Template = template }, null, null);
        }
        catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException)
        {
            logger.LogDebug(nameof(ResourceManagerControlPlane), nameof(ExportDeploymentTemplate),
                "Failed to decode stored template for deployment '{0}': {1}", deploymentName, ex.Message);
            return new ControlPlaneOperationResult<ExportTemplateResult>(OperationResult.Failed, null,
                $"Stored template for deployment '{deploymentName}' is invalid.", "InvalidStoredTemplate");
        }
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

    public ControlPlaneOperationResult<WhatIfOperationResult> WhatIfDeployment(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string deploymentName,
        CreateDeploymentRequest request,
        string location)
    {
        try
        {
            var template = request.ToTemplate();
            var mode = request.Properties?.Mode ?? "Incremental";

            var subscriptionMetadata = new SubscriptionMetadata(subscriptionIdentifier);
            var resourceGroupMetadata =
                new ResourceGroupMetadata(subscriptionIdentifier, resourceGroupIdentifier, location);
            var metadata = new DeploymentMetadata
            {
                { DeploymentMetadata.SubscriptionKey, JToken.Parse(subscriptionMetadata.ToString()) },
                { DeploymentMetadata.ResourceGroupKey, JToken.Parse(resourceGroupMetadata.ToString()) }
            };
            var metadataInsensitive =
                metadata.ToInsensitiveDictionary(meta => meta.Key, meta => meta.Value);

            _templateEngineFacade.ProcessTemplate(subscriptionIdentifier, resourceGroupIdentifier, template,
                metadataInsensitive,
                request.Properties?.Parameters?.Parameters == null ? null
                    : BinaryData.FromObjectAsJson(request.Properties.Parameters.Parameters, GlobalSettings.JsonOptions));

            foreach (var resource in template.Resources)
            {
                resource.Id = new TemplateGenericProperty<string>
                {
                    Value = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/{resource.Type.Value}/{resource.Name.Value}"
                };
            }

            var changes = BuildWhatIfChanges(
                subscriptionIdentifier, resourceGroupIdentifier, template, mode,
                isSubscriptionScope: false);

            return new ControlPlaneOperationResult<WhatIfOperationResult>(
                OperationResult.Success, WhatIfOperationResult.From(changes), null, null);
        }
        catch (Exception ex)
        {
            logger.LogDebug(nameof(ResourceManagerControlPlane), nameof(WhatIfDeployment),
                "What-If analysis failed: {0}", ex.Message);

            return new ControlPlaneOperationResult<WhatIfOperationResult>(
                OperationResult.Failed, null, ex.Message, "WhatIfFailed");
        }
    }

    public ControlPlaneOperationResult<WhatIfOperationResult> WhatIfDeploymentAtSubscriptionScope(
        SubscriptionIdentifier subscriptionIdentifier,
        string deploymentName,
        CreateDeploymentRequest request)
    {
        try
        {
            var template = request.ToTemplate();
            var mode = request.Properties?.Mode ?? "Incremental";

            var subscriptionMetadata = new SubscriptionMetadata(subscriptionIdentifier);
            var metadata = new DeploymentMetadata
            {
                { DeploymentMetadata.SubscriptionKey, JToken.Parse(subscriptionMetadata.ToString()) }
            };
            var metadataInsensitive =
                metadata.ToInsensitiveDictionary(meta => meta.Key, meta => meta.Value);

            _templateEngineFacade.ProcessTemplateAtSubscriptionScope(subscriptionIdentifier, template,
                metadataInsensitive,
                request.Properties?.Parameters?.Parameters == null ? null
                    : BinaryData.FromObjectAsJson(request.Properties.Parameters.Parameters, GlobalSettings.JsonOptions));

            foreach (var resource in template.Resources)
            {
                resource.Id = new TemplateGenericProperty<string>
                {
                    Value = $"/subscriptions/{subscriptionIdentifier}/providers/{resource.Type.Value}/{resource.Name.Value}"
                };
            }

            var changes = BuildWhatIfChanges(
                subscriptionIdentifier, resourceGroupIdentifier: null, template, mode,
                isSubscriptionScope: true);

            return new ControlPlaneOperationResult<WhatIfOperationResult>(
                OperationResult.Success, WhatIfOperationResult.From(changes), null, null);
        }
        catch (Exception ex)
        {
            logger.LogDebug(nameof(ResourceManagerControlPlane), nameof(WhatIfDeploymentAtSubscriptionScope),
                "What-If analysis failed: {0}", ex.Message);

            return new ControlPlaneOperationResult<WhatIfOperationResult>(
                OperationResult.Failed, null, ex.Message, "WhatIfFailed");
        }
    }

    private List<WhatIfChange> BuildWhatIfChanges(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier? resourceGroupIdentifier,
        Template template,
        string mode,
        bool isSubscriptionScope)
    {
        // Build "after" map keyed by the ARM resource ID computed directly from template fields.
        // We do NOT rely on r.ToJson() to include a correctly-typed "id" property because
        // TemplateGenericProperty<string> may serialize as an object rather than a plain string.
        var afterNodes = new Dictionary<string, JsonNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in template.Resources)
        {
            var id = isSubscriptionScope
                ? $"/subscriptions/{subscriptionIdentifier}/providers/{r.Type.Value}/{r.Name.Value}"
                : $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/{r.Type.Value}/{r.Name.Value}";

            var node = JsonNode.Parse(r.ToJson());
            if (node == null) continue;
            
            // Strip unresolved ARM expressions before comparison to prevent false diffs.
            // For example "[tenant().tenantId]" cannot be compared against a stored GUID value.
            StripArmExpressionsFromNode(node);
            afterNodes[id] = node;
        }

        var beforeResources = isSubscriptionScope
            ? new Dictionary<string, GenericResource>(StringComparer.OrdinalIgnoreCase)
            : CollectResourcesFromGroup(subscriptionIdentifier, resourceGroupIdentifier!)
                .Where(r => r.Id != null)
                .ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

        var changes = new List<WhatIfChange>();

        foreach (var (id, afterNode) in afterNodes)
        {
            if (!beforeResources.TryGetValue(id, out var before))
            {
                changes.Add(WhatIfChange.ForCreate(id, afterNode));
            }
            else
            {
                var beforeJson = JsonSerializer.Serialize(before, GlobalSettings.JsonOptions);
                var beforeNode = NormalizeStoredJsonForComparison(JsonNode.Parse(beforeJson)!);
                var delta = WhatIfEngine.ComputeDelta(beforeNode, afterNode);

                if (delta.Count == 0)
                    changes.Add(WhatIfChange.ForNoChange(id, beforeNode));
                else
                    changes.Add(WhatIfChange.ForModify(id, beforeNode, afterNode, delta));
            }
        }

        if (!mode.Equals("Complete", StringComparison.OrdinalIgnoreCase)) return changes;
        {
            foreach (var (id, before) in beforeResources)
            {
                if (afterNodes.ContainsKey(id)) continue;
                
                var beforeNode = JsonNode.Parse(JsonSerializer.Serialize(before, GlobalSettings.JsonOptions))!;
                changes.Add(WhatIfChange.ForDelete(id, beforeNode));
            }
        }

        return changes;
    }

    /// <summary>
    /// Normalizes a resource JSON loaded from Topaz's disk format so that it can be compared
    /// against a template-format resource JSON. Specifically, lifts <c>properties.sku</c> to
    /// the resource level when no resource-level <c>sku</c> is present, matching the ARM
    /// standard representation used by the template engine.
    /// </summary>
    private static JsonNode NormalizeStoredJsonForComparison(JsonNode node)
    {
        if (node is not JsonObject obj)
            return node;

        if (obj["sku"] is null
            && obj["properties"] is JsonObject props
            && props["sku"] is JsonNode skuNode)
        {
            obj["sku"] = skuNode.DeepClone();
            props.Remove("sku");
        }

        return obj;
    }

    /// <summary>
    /// Recursively removes any JSON object property whose value is an unresolved ARM template
    /// expression (a string beginning with '[' and ending with ']').  Prevents false "Modify"
    /// results in What-If comparisons when the template engine could not resolve a function such
    /// as <c>tenant().tenantId</c>.
    /// </summary>
    private static void StripArmExpressionsFromNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
            {
                var toRemove = new List<string>();
                foreach (var (key, value) in obj)
                {
                    if (value is JsonValue v && v.TryGetValue<string>(out var s)
                                             && s.StartsWith('[') && s.EndsWith(']'))
                        toRemove.Add(key);
                    else
                        StripArmExpressionsFromNode(value);
                }
                foreach (var key in toRemove)
                    obj.Remove(key);
                break;
            }
            case JsonArray arr:
            {
                foreach (var item in arr)
                    StripArmExpressionsFromNode(item);
                break;
            }
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