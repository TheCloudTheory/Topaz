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
using Topaz.Service.AppConfiguration;
using Topaz.Service.Insights;
using Topaz.Service.LogAnalytics;
using Topaz.Service.LoadBalancer;
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

    private readonly ArmTemplateEngineFacade _armTemplateEngineFacade = new(logger);

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
                Value = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resourceGroupIdentifier}/providers/{resource.Type.Value}/{resource.Name.Value}"
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
            parameters: deploymentResource.Properties.Parameters,
            setError: error => deploymentResource.Properties.Error = error);

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
                    Value = $"/subscriptions/{subscriptionIdentifier}/resourceGroups/{resource.Name.Value}"
                };
            }
            else
            {
                resource.Id = new TemplateGenericProperty<string>
                {
                    Value = $"/subscriptions/{subscriptionIdentifier}/providers/{resource.Type.Value}/{resource.Name.Value}"
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
            parameters: deploymentResource.Properties.Parameters,
            setError: error => deploymentResource.Properties.Error = error);

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
                Value = $"/providers/{resource.Type.Value}/{resource.Name.Value}"
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
            parameters: deploymentResource.Properties.Parameters,
            setError: error => deploymentResource.Properties.Error = error);

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
                Value = $"/providers/{resource.Type.Value}/{resource.Name.Value}"
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
            parameters: deploymentResource.Properties.Parameters,
            setError: error => deploymentResource.Properties.Error = error);

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
                catch (Exception ex)
                {
                    logger.LogError(nameof(TemplateDeploymentOrchestrator), nameof(Start),
                        "Unhandled exception on deployment background thread for '{0}': {1}", deployment.Id, ex.Message);
                    deployment.SetError(new DeploymentErrorInfo { Code = "DeploymentFailed", Message = ex.Message });
                    deployment.Fail();
                    deployment.Persist();
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

        // Parse scope identifiers from the deployment ID once; used for both property
        // expression evaluation and output evaluation below.
        var idParts = templateDeployment.Id.TrimStart('/').Split('/');
        var deploymentSubscriptionId = idParts.Length > 1 && idParts[0] == "subscriptions" ? idParts[1] : string.Empty;
        var deploymentResourceGroupName = idParts.Length > 3 && idParts[2] == "resourceGroups" ? idParts[3] : string.Empty;

        var hasProvisioningFailed = false;

        // Compute the directory that holds metadata.json (and operations.json) for this deployment.
        // Tenant scope:  /providers/Microsoft.Resources/deployments/{name}      → idParts[0]=="providers", [1]=="Microsoft.Resources"
        // MG scope:      /providers/Microsoft.Management/managementGroups/{id}… → idParts[0]=="providers", [1]=="Microsoft.Management"
        // Sub scope:     /subscriptions/{sub}/providers/…                       → idParts[0]=="subscriptions", rg empty
        // RG scope:      /subscriptions/{sub}/resourceGroups/{rg}/…             → idParts[0]=="subscriptions", rg present
        string operationsDirPath;
        if (idParts[0] == "providers" && idParts.Length > 1 && idParts[1] == "Microsoft.Management")
            operationsDirPath = OperationStore.GetMgScopeDirectory(idParts[3], templateDeployment.Name);
        else if (idParts[0] == "providers")
            operationsDirPath = OperationStore.GetTenantScopeDirectory(templateDeployment.Name);
        else if (string.IsNullOrEmpty(deploymentResourceGroupName))
            operationsDirPath = OperationStore.GetSubScopeDirectory(deploymentSubscriptionId, templateDeployment.Name);
        else
            operationsDirPath = OperationStore.GetRgScopeDirectory(deploymentSubscriptionId, deploymentResourceGroupName, templateDeployment.Name);

        // Process resource groups first to ensure they exist before dependent resources are deployed
        var orderedResources = templateDeployment.Template.Resources
            .OrderByDescending(r => (r.Type?.Value ?? string.Empty).Equals("Microsoft.Resources/resourceGroups", StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        foreach (var resource in orderedResources)
        {
            // Evaluate any remaining ARM expressions in resource.Properties before
            // serializing to GenericResource. The template engine resolves variables
            // lazily and does not inline them into the raw properties JToken.
            _armTemplateEngineFacade.EvaluateResourceProperties(
                deploymentSubscriptionId, deploymentResourceGroupName,
                templateDeployment.Template, resource);

            IControlPlane? controlPlane = null;
            var genericResource =
                JsonSerializer.Deserialize<GenericResource>(resource.ToJson(), GlobalSettings.JsonOptions)!;

            // resource.Type.Value may be null after subscription-scope template processing for
            // certain resource types (e.g. Microsoft.Resources/deployments); fall back to the
            // type string preserved in the deserialized GenericResource.
            var resourceType = resource.Type?.Value ?? genericResource.Type;
            var normalizedResourceType = resourceType.ToLowerInvariant();

            switch (normalizedResourceType)
            {
                case "microsoft.containerregistry/registries":
                    controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.keyvault/vaults":
                    controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.network/virtualnetworks":
                    controlPlane = VirtualNetworkControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.network/networksecuritygroups":
                    controlPlane = NetworkSecurityGroupControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.network/networkinterfaces":
                    controlPlane = NetworkInterfaceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.network/privateendpoints":
                    controlPlane = PrivateEndpointControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.network/publicipaddresses":
                    controlPlane = PublicIpAddressControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.network/loadbalancers":
                    controlPlane = LoadBalancerControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.compute/virtualmachines":
                    controlPlane = VirtualMachineServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.compute/disks":
                    controlPlane = DiskServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.appconfiguration/configurationstores":
                    controlPlane = AppConfigurationServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.operationalinsights/workspaces":
                    controlPlane = LogAnalyticsServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.insights/components":
                    controlPlane = ApplicationInsightsServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.managedidentity/userassignedidentities":
                    controlPlane = ManagedIdentityControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.eventhub/namespaces":
                    controlPlane = EventHubServiceControlPlane.New(logger);
                    break;
                case "microsoft.servicebus/namespaces":
                    controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.storage/storageaccounts":
                    controlPlane = AzureStorageControlPlane.New(logger);
                    break;
                case "microsoft.web/serverfarms":
                    controlPlane = AppServicePlanControlPlane.New(logger);
                    break;
                case "microsoft.web/sites":
                    controlPlane = AppServiceSiteControlPlane.New(logger);
                    break;
                case "microsoft.sql/servers":
                    controlPlane = SqlServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.sql/servers/databases":
                    controlPlane = SqlServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.documentdb/databaseaccounts":
                    controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.resources/resourcegroups":
                    controlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);
                    break;
                case "microsoft.resources/deployments":
                    HandleNestedDeployment(genericResource, templateDeployment, resource, ref hasProvisioningFailed);
                    break;
                default:
                    logger.LogWarning($"Deployment of resource type {resourceType} is not yet supported.");
                    break;
            }

            var operationStart = DateTimeOffset.UtcNow;
            var result = controlPlane?.Deploy(genericResource);
            logger.LogInformation($"Deployment of {genericResource.Id} completed with status {result}.");

            if (controlPlane != null)
            {
                var record = Models.OperationRecord.Create(
                    templateDeployment.Id,
                    genericResource.Id,
                    resourceType,
                    genericResource.Name,
                    succeeded: result != OperationResult.Failed,
                    start: operationStart,
                    end: DateTimeOffset.UtcNow);
                OperationStore.Append(operationsDirPath, record);
            }

            if (result == OperationResult.Failed)
                hasProvisioningFailed = true;

            if (!templateDeployment.CancellationToken.IsCancellationRequested) continue;
            
            templateDeployment.Cancel();
            templateDeployment.Persist();
            logger.LogInformation($"Deployment {templateDeployment.Id} was cancelled mid-flight after provisioning {genericResource.Id}.");
            return;
        }

        // Evaluate and set template outputs on the deployment
        if (templateDeployment.Template.Outputs != null)
        {
            JToken? ReferenceResolver(string expr) => ResolveNestedDeploymentOutput(expr, templateDeployment) ??
                                                      ReferenceExpressionResolver.TryResolve(expr,
                                                          deploymentSubscriptionId, deploymentResourceGroupName,
                                                          GlobalSettings.MainEmulatorDirectory);

            var outputsJObject = _armTemplateEngineFacade.EvaluateOutputs(
                deploymentSubscriptionId, deploymentResourceGroupName,
                templateDeployment.Template, logger,
                ReferenceResolver,
                templateDeployment.SymbolicNameMap.Count > 0 ? templateDeployment.SymbolicNameMap : null);
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
                : null;
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
            else if (parentDeployment.Metadata.TryGetValue(DeploymentMetadata.LocationKey, out var parentLocationToken)
                     && parentLocationToken.Type == JTokenType.String
                     && !string.IsNullOrWhiteSpace(parentLocationToken.Value<string>()))
            {
                // Subscription-scoped parent deployments carry the location directly (no resource group metadata).
                nestedLocation = new AzureLocation(parentLocationToken.Value<string>()!);
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
                { DeploymentMetadata.ResourceGroupKey, JToken.Parse(resourceGroupMetadata.ToString()) },
                { DeploymentMetadata.TenantKey, JToken.Parse(new TenantMetadata().ToString()) }
            };
            var innerMetadata = innerMetadataDict.ToInsensitiveDictionary(x => x.Key, x => x.Value);

            // Step 4: Parse and process inner template
            var innerTemplate = _armTemplateEngineFacade.Parse(innerTemplateJson);

            // Process template expressions first so Type/Name are resolved before ID assignment
            _armTemplateEngineFacade.ProcessTemplate(nestedSubId, nestedRgId, innerTemplate, innerMetadata, innerParams);

            // Assign resource IDs on inner template resources (after expression processing)
            foreach (var innerResource in innerTemplate.Resources)
            {
                innerResource.Id = new TemplateGenericProperty<string>
                {
                    Value = $"/subscriptions/{nestedSubId}/resourceGroups/{nestedRgId}/providers/{innerResource.Type}/{innerResource.Name}"
                };
            }

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
                parameters: innerParams,
                setError: error => nestedDeploymentResource.Properties.Error = error);

            innerJob.SetCancellationTokenSource(linkedCts);

            // If the inner template uses dict-style resources (Bicep newer format), build a
            // symbolic-name → resource-type map so that reference('symbolName') expressions
            // in outputs can be resolved at evaluation time.
            if (templateElement.TryGetProperty("resources", out var innerResElement) &&
                innerResElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in innerResElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.Object ||
                        !prop.Value.TryGetProperty("type", out var typeProp) ||
                        typeProp.ValueKind != JsonValueKind.String) continue;
                    var resourceType = typeProp.GetString();
                    if (!string.IsNullOrEmpty(resourceType))
                        innerJob.SymbolicNameMap[prop.Name] = resourceType;
                }
            }

            // Step 7: Recursive execution + status propagation
            try
            {
                RouteDeployment(innerJob);
            }
            catch (Exception ex)
            {
                logger.LogError(nameof(TemplateDeploymentOrchestrator), nameof(HandleNestedDeployment),
                    "Unhandled exception in nested deployment '{0}': {1}", genericResource.Name, ex.Message);
                nestedDeploymentResource.Properties.Error = new DeploymentErrorInfo { Code = "DeploymentFailed", Message = ex.Message };
                innerJob.Fail();
                innerJob.Persist();
                hasProvisioningFailed = true;
                linkedCts.Dispose();
                return;
            }

            if (innerJob.Status == TemplateDeployment.DeploymentStatus.Failed || 
                innerJob.Status == TemplateDeployment.DeploymentStatus.Cancelled)
            {
                hasProvisioningFailed = true;
            }
            else
            {
                // Propagate nested deployment outputs back to the parent so that
                // reference('<deploymentName>').outputs.x.value expressions resolve.
                parentDeployment.NestedDeploymentOutputs[genericResource.Name] =
                    nestedDeploymentResource.Properties.Outputs;
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

    // Matches 'Microsoft.Resources/deployments', '<deploymentName>' anywhere in a reference() expression
    private static readonly System.Text.RegularExpressions.Regex NestedDeploymentNamePattern = new(
        @"'Microsoft\.Resources/deployments',\s*'([^']+)'",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase |
        System.Text.RegularExpressions.RegexOptions.Compiled);

    // Matches .outputs.<key>.value at the end of an ARM output expression
    private static readonly System.Text.RegularExpressions.Regex DeploymentOutputKeyPattern = new(
        @"\.outputs\.([^.\]\s]+)\.value\]?$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase |
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Resolves <c>reference(extensionResourceId(..., 'Microsoft.Resources/deployments', '&lt;name&gt;'), ...).outputs.&lt;key&gt;.value</c>
    /// expressions using nested deployment outputs collected during this deployment run.
    /// Returns <c>null</c> when the expression doesn't match or the output isn't available.
    /// </summary>
    private static JToken? ResolveNestedDeploymentOutput(string expression, TemplateDeployment deployment)
    {
        var expr = expression.Trim();
        if (!expr.Contains("reference(", StringComparison.OrdinalIgnoreCase)) return null;
        if (!expr.Contains("Microsoft.Resources/deployments", StringComparison.OrdinalIgnoreCase)) return null;

        var nameMatch = NestedDeploymentNamePattern.Match(expr);
        if (!nameMatch.Success) return null;

        var keyMatch = DeploymentOutputKeyPattern.Match(expr);
        if (!keyMatch.Success) return null;

        var deploymentName = nameMatch.Groups[1].Value;
        var outputKey = keyMatch.Groups[1].Value;

        if (!deployment.NestedDeploymentOutputs.TryGetValue(deploymentName, out var outputs) || outputs == null)
            return null;

        // output is a JsonElement shaped as { keyVaultName: { type: "string", value: "..." }, ... }
        if (outputs.Value.TryGetProperty(outputKey, out var outputEntry) &&
            outputEntry.TryGetProperty("value", out var valueElement))
        {
            return JToken.Parse(valueElement.GetRawText());
        }

        return null;
    }
}