using System.Text.Json;
using Azure.Core;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Entities;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json.Linq;
using Topaz.EventPipeline;
using Topaz.ResourceManager;
using Topaz.Service.AppService;
using Topaz.Service.ContainerRegistry;
using Topaz.Service.EventHub;
using Topaz.Service.KeyVault;
using Topaz.Service.ManagedIdentity;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceGroup.Models;
using Topaz.Service.ServiceBus;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage;
using Topaz.Service.Sql;
using Topaz.Service.CosmosDb;
using Topaz.Service.Disk;
using Topaz.Service.VirtualMachine;
using Topaz.Service.VirtualNetwork;
using Topaz.Shared;
using DeploymentResource = Topaz.Service.ResourceManager.Models.DeploymentResource;

namespace Topaz.Service.ResourceManager.Deployment;

public sealed class TemplateDeploymentOrchestrator(
    Pipeline eventPipeline,
    ResourceManagerResourceProvider rgProvider,
    SubscriptionDeploymentResourceProvider subProvider,
    TenantDeploymentResourceProvider tenantProvider,
    ManagementGroupDeploymentResourceProvider mgProvider,
    ITopazLogger logger)
{
    private static readonly List<TemplateDeployment> DeploymentQueue = [];
    private static readonly Lock QueueLock = new();
    private static string? _currentDeploymentId;
    private static CancellationTokenSource? _currentCts;
    private static Thread? OrchestratorThread { get; set; }

    private readonly ArmTemplateEngineFacade _armTemplateEngineFacade = new();

    public void EnqueueTemplateDeployment(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        Template template,
        DeploymentResource deploymentResource,
        InsensitiveDictionary<JToken> metadataInsensitive)
    {
        _armTemplateEngineFacade.ProcessTemplate(subscriptionIdentifier, resourceGroupIdentifier, template,
            metadataInsensitive, deploymentResource.Properties.Parameters);

        foreach (var resource in template.Resources)
        {
            resource.Id = new TemplateGenericProperty<string>
            {
                Value = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/{resource.Type}/{resource.Name}"
            };
        }

        var job = new TemplateDeployment(
            deploymentResource.Id, deploymentResource.Name, template,
            complete: deploymentResource.CompleteDeployment,
            cancel: deploymentResource.CancelDeployment,
            fail: deploymentResource.FailDeployment,
            persist: () => rgProvider.CreateOrUpdate(subscriptionIdentifier, resourceGroupIdentifier,
                deploymentResource.Name, deploymentResource),
            setOutputs: outputs => deploymentResource.Properties.Outputs = outputs,
            metadata: metadataInsensitive,
            parameters: deploymentResource.Properties.Parameters);

        lock (QueueLock) { DeploymentQueue.Add(job); }
    }

    public void EnqueueSubscriptionDeployment(
        SubscriptionIdentifier subscriptionIdentifier,
        Template template,
        SubscriptionDeploymentResource deploymentResource,
        InsensitiveDictionary<JToken> metadataInsensitive)
    {
        _armTemplateEngineFacade.ProcessTemplateAtSubscriptionScope(subscriptionIdentifier, template,
            metadataInsensitive, deploymentResource.Properties.Parameters);

        foreach (var resource in template.Resources)
        {
            // Resource groups use /subscriptions/{sub}/resourceGroups/{name} path format
            if (resource.Type.Value == "Microsoft.Resources/resourceGroups")
            {
                resource.Id = new TemplateGenericProperty<string>
                {
                    Value = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resource.Name}"
                };
            }
            else
            {
                resource.Id = new TemplateGenericProperty<string>
                {
                    Value = $"/subscriptions/{subscriptionIdentifier}/providers/{resource.Type}/{resource.Name}"
                };
            }
        }

        var job = new TemplateDeployment(
            deploymentResource.Id, deploymentResource.Name, template,
            complete: deploymentResource.CompleteDeployment,
            cancel: deploymentResource.CancelDeployment,
            fail: deploymentResource.FailDeployment,
            persist: () => subProvider.CreateOrUpdate(subscriptionIdentifier, null,
                deploymentResource.Name, deploymentResource),
            setOutputs: outputs => deploymentResource.Properties.Outputs = outputs,
            metadata: metadataInsensitive,
            parameters: deploymentResource.Properties.Parameters);

        lock (QueueLock) { DeploymentQueue.Add(job); }
    }

    public void EnqueueTenantDeployment(
        Template template,
        TenantDeploymentResource deploymentResource,
        InsensitiveDictionary<JToken> metadataInsensitive)
    {
        _armTemplateEngineFacade.ProcessTemplateAtTenantScope(template,
            metadataInsensitive, deploymentResource.Properties.Parameters);

        foreach (var resource in template.Resources)
        {
            resource.Id = new TemplateGenericProperty<string>
            {
                Value = $"/providers/{resource.Type}/{resource.Name}"
            };
        }

        var job = new TemplateDeployment(
            deploymentResource.Id, deploymentResource.Name, template,
            complete: deploymentResource.CompleteDeployment,
            cancel: deploymentResource.CancelDeployment,
            fail: deploymentResource.FailDeployment,
            persist: () => tenantProvider.CreateOrUpdateDeployment(deploymentResource.Name, deploymentResource),
            setOutputs: outputs => deploymentResource.Properties.Outputs = outputs,
            metadata: metadataInsensitive,
            parameters: deploymentResource.Properties.Parameters);

        lock (QueueLock) { DeploymentQueue.Add(job); }
    }

    public void EnqueueManagementGroupDeployment(
        string groupId,
        Template template,
        ManagementGroupDeploymentResource deploymentResource,
        InsensitiveDictionary<JToken> metadataInsensitive)
    {
        _armTemplateEngineFacade.ProcessTemplateAtManagementGroupScope(template,
            metadataInsensitive, deploymentResource.Properties.Parameters);

        foreach (var resource in template.Resources)
        {
            resource.Id = new TemplateGenericProperty<string>
            {
                Value = $"/providers/{resource.Type}/{resource.Name}"
            };
        }

        var job = new TemplateDeployment(
            deploymentResource.Id, deploymentResource.Name, template,
            complete: deploymentResource.CompleteDeployment,
            cancel: deploymentResource.CancelDeployment,
            fail: deploymentResource.FailDeployment,
            persist: () => mgProvider.CreateOrUpdateDeployment(groupId, deploymentResource.Name, deploymentResource),
            setOutputs: outputs => deploymentResource.Properties.Outputs = outputs,
            metadata: metadataInsensitive,
            parameters: deploymentResource.Properties.Parameters);

        lock (QueueLock) { DeploymentQueue.Add(job); }
    }


    public OperationResult CancelDeployment(string deploymentId)
    {
        TemplateDeployment? toCancel;
        lock (QueueLock)
        {
            if (_currentDeploymentId == deploymentId)
            {
                // Signal the running deployment's CancellationToken; RouteDeployment will
                // detect it after the current resource completes and transition to Canceled.
                _currentCts?.Cancel();
                return OperationResult.Success;
            }

            toCancel = DeploymentQueue.FirstOrDefault(d => d.Id == deploymentId);
            if (toCancel == null)
                return OperationResult.Conflict;

            DeploymentQueue.RemoveAll(d => d.Id == deploymentId);
        }

        toCancel.Cancel();
        toCancel.Persist();
        return OperationResult.Success;
    }

    public void Start(CancellationToken stoppingToken = default)
    {
        if (OrchestratorThread != null)
            throw new InvalidOperationException("Orchestrator thread already running");

        OrchestratorThread = new Thread(() =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TemplateDeployment? deployment = null;
                lock (QueueLock)
                {
                    if (DeploymentQueue.Count > 0)
                    {
                        deployment = DeploymentQueue[0];
                        DeploymentQueue.RemoveAt(0);
                        _currentDeploymentId = deployment.Id;
                    }
                }

                if (deployment == null)
                {
                    logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(Start),"No deployments in the queue, will attempt to check again in 10 seconds...");
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    continue;
                }

                logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(Start),
                    "Fetched deployment: {0}", deployment.Id);

                var cts = new CancellationTokenSource();
                deployment.SetCancellationTokenSource(cts);
                lock (QueueLock) { _currentCts = cts; }

                try
                {
                    RouteDeployment(deployment);
                }
                finally
                {
                    lock (QueueLock)
                    {
                        _currentDeploymentId = null;
                        _currentCts = null;
                    }
                    cts.Dispose();
                }
            }
        });

        OrchestratorThread.Start();
    }

    private void RouteDeployment(TemplateDeployment templateDeployment)
    {
        logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(RouteDeployment),
            "Routing deployment resources of {0} to appropriate control planes.", templateDeployment.Id);

        templateDeployment.Start();
        logger.LogInformation($"Deployment of {templateDeployment.Id} started.");

        var hasProvisioningFailed = false;
        // Process resource groups first to ensure they exist before dependent resources are deployed
        var orderedResources = templateDeployment.Template.Resources
            .OrderByDescending(r => r.Type.Value == "Microsoft.Resources/resourceGroups")
            .ToList();
        
        foreach (var resource in orderedResources)
        {
            IControlPlane? controlPlane = null;
            var genericResource =
                JsonSerializer.Deserialize<GenericResource>(resource.ToJson(), GlobalSettings.JsonOptions)!;

            switch (resource.Type.Value)
            {
                case "Microsoft.ContainerRegistry/registries":
                    controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.KeyVault/vaults":
                    controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Network/virtualNetworks":
                    controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Network/networkSecurityGroups":
                    controlPlane = NetworkSecurityGroupControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Network/networkInterfaces":
                    controlPlane = NetworkInterfaceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Network/publicIPAddresses":
                    controlPlane = PublicIpAddressControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Compute/virtualMachines":
                    controlPlane = VirtualMachineServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Compute/disks":
                    controlPlane = DiskServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.ManagedIdentity/userAssignedIdentities":
                    controlPlane = ManagedIdentityControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.EventHub/namespaces":
                    controlPlane = EventHubServiceControlPlane.New(logger);
                    break;
                case "Microsoft.ServiceBus/namespaces":
                    controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Storage/storageAccounts":
                    controlPlane = AzureStorageControlPlane.New(logger);
                    break;
                case "Microsoft.Web/serverfarms":
                    controlPlane = AppServicePlanControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Web/sites":
                    controlPlane = AppServiceSiteControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Sql/servers":
                    controlPlane = SqlServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Sql/servers/databases":
                    controlPlane = SqlServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.DocumentDB/databaseAccounts":
                    controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Resources/resourceGroups":
                    controlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);
                    break;
                case "Microsoft.Resources/deployments":
                    HandleNestedDeployment(genericResource, templateDeployment, resource, ref hasProvisioningFailed);
                    break;
                default:
                    logger.LogWarning($"Deployment of resource type {resource.Type} is not yet supported.");
                    break;
            }

            var result = controlPlane?.Deploy(genericResource);
            logger.LogInformation($"Deployment of {genericResource.Id} completed with status {result}.");

            if (result == OperationResult.Failed)
                hasProvisioningFailed = true;

            if (templateDeployment.CancellationToken.IsCancellationRequested)
            {
                templateDeployment.Cancel();
                templateDeployment.Persist();
                logger.LogInformation($"Deployment {templateDeployment.Id} was cancelled mid-flight after provisioning {genericResource.Id}.");
                return;
            }
        }

        // Evaluate and set template outputs on the deployment
        if (templateDeployment.Template.Outputs != null)
        {
            // Parse subscription ID and resource group name from the deployment ID so the
            // expression evaluation context matches the scope used during template processing.
            var idParts = templateDeployment.Id.TrimStart('/').Split('/');
            var subscriptionId = idParts.Length > 1 && idParts[0] == "subscriptions" ? idParts[1] : string.Empty;
            var resourceGroupName = idParts.Length > 3 && idParts[2] == "resourceGroups" ? idParts[3] : string.Empty;

            var outputsJObject = _armTemplateEngineFacade.EvaluateOutputs(subscriptionId, resourceGroupName, templateDeployment.Template);
            var outputsJson = outputsJObject.ToString(Newtonsoft.Json.Formatting.None);
            var outputs = JsonDocument.Parse(outputsJson).RootElement.Clone();
            templateDeployment.SetOutputs(outputs);
        }

        if (!hasProvisioningFailed)
            templateDeployment.Complete();
        else
            templateDeployment.Fail();

        templateDeployment.Persist();
        logger.LogInformation($"Deployment {templateDeployment.Id} completed.");
    }

    /// <summary>
    /// Extracts the evaluated output values from template outputs.
    /// After ProcessTemplateLanguageExpressions, the output values are evaluated in place.
    /// This method builds the outputs in the format { type, value } that Azure expects.
    /// </summary>
    private static Dictionary<string, object?> ExtractEvaluatedOutputs(InsensitiveDictionary<TemplateOutputParameter> outputs)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var output in outputs)
        {
            var extractedOutput = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            
            // Get type and value from TemplateOutputParameter
            // Both Type and Value are TemplateGenericProperty<T> wrappers
            if (output.Value.Type?.Value != null)
            {
                extractedOutput["type"] = output.Value.Type.Value.ToString().ToLowerInvariant();
            }
            else
            {
                extractedOutput["type"] = "object";
            }
            
            // The value is stored in output.Value.Value which is TemplateGenericProperty<JToken>
            // We need to extract the JToken from it
            var valueGenericProperty = output.Value.Value;
            if (valueGenericProperty != null)
            {
                // Try to get the actual value
                var jtoken = valueGenericProperty.Value;
                if (jtoken != null)
                {
                    // The JToken may contain a value at a nested property
                    // Check if it's structured as { value: {...}, type: {...}, ... }
                    if (jtoken is JObject jo && jo.TryGetValue("value", out var innerValue))
                    {
                        // Extract the inner value
                        extractedOutput["value"] = innerValue;
                    }
                    else
                    {
                        // Use the JToken directly
                        extractedOutput["value"] = jtoken;
                    }
                }
            }
            
            result[output.Key] = extractedOutput;
        }
        
        return result;
    }

    private void HandleNestedDeployment(
        GenericResource genericResource,
        TemplateDeployment parentDeployment,
        TemplateResource resource,
        ref bool hasProvisioningFailed)
    {
        try
        {
            // Step 1: Parse raw resource JSON to extract nested template and context
            var resourceJson = JsonSerializer.Deserialize<JsonElement>(resource.ToJson(), GlobalSettings.JsonOptions);
            var resourceObj = resourceJson.Deserialize<Dictionary<string, JsonElement>>(GlobalSettings.JsonOptions);
            
            if (resourceObj == null)
            {
                logger.LogWarning($"Failed to parse nested deployment resource JSON for '{genericResource.Name}'.");
                hasProvisioningFailed = true;
                return;
            }

            // Extract resourceGroup property
            if (!resourceObj.TryGetValue("resourceGroup", out var rgElement))
            {
                logger.LogWarning($"Nested deployment '{genericResource.Name}' has no 'resourceGroup' property; subscription-scoped nested deployments are not yet supported.");
                return;
            }

            var nestedRgName = rgElement.GetString();
            if (string.IsNullOrWhiteSpace(nestedRgName))
            {
                logger.LogWarning($"Nested deployment '{genericResource.Name}' has empty 'resourceGroup' property.");
                hasProvisioningFailed = true;
                return;
            }

            // Extract properties block
            if (!resourceObj.TryGetValue("properties", out var propsElement) || propsElement.ValueKind != JsonValueKind.Object)
            {
                logger.LogWarning($"Nested deployment '{genericResource.Name}' has no 'properties' object.");
                hasProvisioningFailed = true;
                return;
            }

            var propsObj = propsElement.Deserialize<Dictionary<string, JsonElement>>(GlobalSettings.JsonOptions);
            if (propsObj == null)
            {
                logger.LogWarning($"Failed to deserialize properties of nested deployment '{genericResource.Name}'.");
                hasProvisioningFailed = true;
                return;
            }

            // Extract inner template
            if (!propsObj.TryGetValue("template", out var templateElement))
            {
                logger.LogWarning($"Nested deployment '{genericResource.Name}' has no 'template' in properties.");
                hasProvisioningFailed = true;
                return;
            }

            var innerTemplateJson = templateElement.GetRawText();
            
            // Extract optional parameters and mode
            JsonElement? innerParams = propsObj.TryGetValue("parameters", out var paramsElement) 
                ? paramsElement 
                : (JsonElement?)null;
            var innerMode = propsObj.TryGetValue("mode", out var modeElement) 
                ? modeElement.GetString() ?? "Incremental" 
                : "Incremental";

            // Step 2: Resolve nested context identifiers
            var parentIdParts = parentDeployment.Id.TrimStart('/').Split('/');
            var nestedSubId = parentIdParts.Length > 1 && parentIdParts[0] == "subscriptions"
                ? SubscriptionIdentifier.From(parentIdParts[1])
                : throw new InvalidOperationException($"Cannot extract subscription ID from parent deployment ID: {parentDeployment.Id}");

            var nestedRgId = ResourceGroupIdentifier.From(nestedRgName);

            // Step 3: Build inner metadata
            var subscriptionMetadata = new SubscriptionMetadata(nestedSubId);
            
            // Extract parent RG metadata to get location
            var parentRgMetadata = parentDeployment.Metadata.TryGetValue(DeploymentMetadata.ResourceGroupKey, out var rgMetadataToken)
                ? JsonSerializer.Deserialize<ResourceGroupMetadata>(rgMetadataToken.ToString(), GlobalSettings.JsonOptions)
                : null;

            AzureLocation nestedLocation;
            if (!string.IsNullOrWhiteSpace(genericResource.Location))
            {
                nestedLocation = new AzureLocation(genericResource.Location);
            }
            else if (parentRgMetadata?.Location != null)
            {
                nestedLocation = parentRgMetadata.Location;
            }
            else
            {
                logger.LogError(nameof(TemplateDeploymentOrchestrator), nameof(HandleNestedDeployment),
                    $"Nested deployment '{genericResource.Name}' has no location and parent location cannot be resolved.");
                hasProvisioningFailed = true;
                return;
            }

            var resourceGroupMetadata = new ResourceGroupMetadata(nestedSubId, nestedRgId, nestedLocation);

            var innerMetadataDict = new Dictionary<string, JToken>
            {
                { DeploymentMetadata.SubscriptionKey, JToken.Parse(subscriptionMetadata.ToString()) },
                { DeploymentMetadata.ResourceGroupKey, JToken.Parse(resourceGroupMetadata.ToString()) }
            };
            var innerMetadata = innerMetadataDict.ToInsensitiveDictionary(x => x.Key, x => x.Value);

            // Step 4: Parse and process inner template
            var innerTemplate = _armTemplateEngineFacade.Parse(innerTemplateJson);

            // Assign resource IDs on inner template resources
            foreach (var innerResource in innerTemplate.Resources)
            {
                innerResource.Id = new TemplateGenericProperty<string>
                {
                    Value = $"/subscriptions/{nestedSubId}/resourceGroups/{nestedRgId}/providers/{innerResource.Type}/{innerResource.Name}"
                };
            }

            // Process template expressions
            _armTemplateEngineFacade.ProcessTemplate(nestedSubId, nestedRgId, innerTemplate, innerMetadata, innerParams);

            // Step 5: Create and persist nested DeploymentResource
            var nestedDeploymentProps = DeploymentResourceProperties.New(innerMode, innerTemplateJson, null);
            var nestedDeploymentResource = new DeploymentResource(nestedSubId, nestedRgId, genericResource.Name, nestedLocation, nestedDeploymentProps);
            
            rgProvider.CreateOrUpdate(nestedSubId, nestedRgId, genericResource.Name, nestedDeploymentResource);

            logger.LogDebug(nameof(TemplateDeploymentOrchestrator), nameof(HandleNestedDeployment),
                "Created nested deployment resource '{0}'.", nestedDeploymentResource.Id);

            // Step 6: Build inner TemplateDeployment with linked cancellation
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentDeployment.CancellationToken);
            
            var innerJob = new TemplateDeployment(
                nestedDeploymentResource.Id,
                nestedDeploymentResource.Name,
                innerTemplate,
                complete: nestedDeploymentResource.CompleteDeployment,
                cancel: nestedDeploymentResource.CancelDeployment,
                fail: nestedDeploymentResource.FailDeployment,
                persist: () => rgProvider.CreateOrUpdate(nestedSubId, nestedRgId, genericResource.Name, nestedDeploymentResource),
                setOutputs: outputs => nestedDeploymentResource.Properties.Outputs = outputs,
                metadata: innerMetadata,
                parameters: innerParams);

            innerJob.SetCancellationTokenSource(linkedCts);

            // Step 7: Recursive execution + status propagation
            RouteDeployment(innerJob);

            if (innerJob.Status == TemplateDeployment.DeploymentStatus.Failed || 
                innerJob.Status == TemplateDeployment.DeploymentStatus.Cancelled)
            {
                hasProvisioningFailed = true;
            }

            linkedCts.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogError(nameof(TemplateDeploymentOrchestrator), nameof(HandleNestedDeployment),
                "Failed to handle nested deployment '{0}': {1}", genericResource.Name, ex.Message);
            hasProvisioningFailed = true;
        }
    }
}