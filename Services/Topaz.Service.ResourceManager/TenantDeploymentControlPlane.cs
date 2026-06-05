using System.Text;
using System.Text.Json.Nodes;
using Azure.ResourceManager.Resources.Models;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json.Linq;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager;

internal sealed class TenantDeploymentControlPlane(
    TenantDeploymentResourceProvider provider,
    TemplateDeploymentOrchestrator orchestrator,
    ITopazLogger logger)
{
    private const string DeploymentNotFoundMessageTemplate = "Deployment {0} not found";
    private const string DeploymentNotFoundCode = "DeploymentNotFound";

    private readonly ArmTemplateEngineFacade _templateEngineFacade = new();

    public ControlPlaneOperationResult<TenantDeploymentResource[]> List()
    {
        logger.LogDebug(nameof(TenantDeploymentControlPlane), nameof(List),
            "Listing tenant-scope deployments.");

        var resources = provider.ListDeployments().ToArray();

        return new ControlPlaneOperationResult<TenantDeploymentResource[]>(
            OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<TenantDeploymentResource> CreateOrUpdate(
        string deploymentName,
        string content,
        Dictionary<string, CreateDeploymentRequest.ParameterValue>? parameters,
        string location,
        string mode)
    {
        var template = _templateEngineFacade.Parse(content);
        var deploymentResource = new TenantDeploymentResource(
            deploymentName,
            location,
            DeploymentResourceProperties.New(mode, content, parameters));

        provider.CreateOrUpdateDeployment(deploymentName, deploymentResource);

        orchestrator.EnqueueTenantDeployment(template, deploymentResource,
            InsensitiveDictionary<JToken>.Empty);

        return new ControlPlaneOperationResult<TenantDeploymentResource>(
            OperationResult.Success, deploymentResource, null, null);
    }

    public ControlPlaneOperationResult<TenantDeploymentResource> Get(string deploymentName)
    {
        logger.LogDebug(nameof(TenantDeploymentControlPlane), nameof(Get),
            "Getting tenant-scope deployment `{0}`.", deploymentName);

        var resource = provider.GetDeployment(deploymentName);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<TenantDeploymentResource>(
                OperationResult.NotFound, null,
                string.Format(DeploymentNotFoundMessageTemplate, deploymentName),
                DeploymentNotFoundCode);
        }

        return new ControlPlaneOperationResult<TenantDeploymentResource>(
            OperationResult.Success, resource, null, null);
    }

    public OperationResult Delete(string deploymentName)
    {
        logger.LogDebug(nameof(TenantDeploymentControlPlane), nameof(Delete),
            "Deleting tenant-scope deployment `{0}`.", deploymentName);

        var resource = provider.GetDeployment(deploymentName);
        if (resource == null)
            return OperationResult.NotFound;

        provider.DeleteDeployment(deploymentName);
        return OperationResult.Deleted;
    }

    public OperationResult CancelDeployment(string deploymentName)
    {
        var deploymentOp = Get(deploymentName);
        if (deploymentOp.Result == OperationResult.NotFound)
            return OperationResult.NotFound;

        return orchestrator.CancelDeployment(
            $"/providers/Microsoft.Resources/deployments/{deploymentName}");
    }

    public ControlPlaneOperationResult<DeploymentValidateResult> ValidateDeployment(
        string deploymentName,
        CreateDeploymentRequest request)
    {
        try
        {
            var template = request.ToTemplate();
            _templateEngineFacade.Validate(template);

            return new ControlPlaneOperationResult<DeploymentValidateResult>(OperationResult.Success,
                DeploymentValidateResult.FromRequestAtTenantScope(deploymentName, request),
                null, null);
        }
        catch (Exception ex)
        {
            logger.LogDebug(nameof(TenantDeploymentControlPlane), nameof(ValidateDeployment),
                "Template validation failed: {0}", ex.Message);

            return new ControlPlaneOperationResult<DeploymentValidateResult>(OperationResult.Failed,
                null, ex.Message, "InvalidTemplate");
        }
    }

    public ControlPlaneOperationResult<ExportTemplateResult> ExportDeploymentTemplate(string deploymentName)
    {
        var deploymentOp = Get(deploymentName);
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
            logger.LogDebug(nameof(TenantDeploymentControlPlane), nameof(ExportDeploymentTemplate),
                "Failed to decode stored template for deployment '{0}': {1}", deploymentName, ex.Message);
            return new ControlPlaneOperationResult<ExportTemplateResult>(OperationResult.Failed, null,
                $"Stored template for deployment '{deploymentName}' is invalid.", "InvalidStoredTemplate");
        }
    }
}
