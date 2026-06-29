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

internal sealed class ManagementGroupDeploymentControlPlane(
    ManagementGroupDeploymentResourceProvider provider,
    TemplateDeploymentOrchestrator orchestrator,
    ArmTemplateEngineFacade templateEngineFacade,
    ITopazLogger logger)
{
    private readonly ArmTemplateEngineFacade _templateEngineFacade = templateEngineFacade;
    private const string NotFoundCode = "ManagementGroupNotFound";
    private const string NotFoundMessageTemplate = "Management group '{0}' was not found.";
    private const string DeploymentNotFoundCode = "DeploymentNotFound";
    private const string DeploymentNotFoundMessageTemplate = "Deployment '{0}' was not found.";

    public ControlPlaneOperationResult<ManagementGroupDeploymentResource[]> List(string groupId)
    {
        logger.LogDebug(nameof(ManagementGroupDeploymentControlPlane), nameof(List),
            "Listing management-group-scope deployments for management group '{0}'.", groupId);

        if (!provider.ManagementGroupExists(groupId))
        {
            return new ControlPlaneOperationResult<ManagementGroupDeploymentResource[]>(
                OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId),
                NotFoundCode);
        }

        var resources = provider.ListDeployments(groupId)
            .Where(d => d.IsInManagementGroup(groupId))
            .ToArray();

        return new ControlPlaneOperationResult<ManagementGroupDeploymentResource[]>(
            OperationResult.Success, resources, null, null);
    }

    public ControlPlaneOperationResult<ManagementGroupDeploymentResource> Get(string groupId, string deploymentName)
    {
        logger.LogDebug(nameof(ManagementGroupDeploymentControlPlane), nameof(Get),
            "Getting management-group-scope deployment '{0}' in management group '{1}'.",
            deploymentName, groupId);

        if (!provider.ManagementGroupExists(groupId))
        {
            return new ControlPlaneOperationResult<ManagementGroupDeploymentResource>(
                OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId),
                NotFoundCode);
        }

        var resource = provider.GetDeployment(groupId, deploymentName);
        if (resource == null)
        {
            return new ControlPlaneOperationResult<ManagementGroupDeploymentResource>(
                OperationResult.NotFound, null,
                string.Format(DeploymentNotFoundMessageTemplate, deploymentName),
                DeploymentNotFoundCode);
        }

        return new ControlPlaneOperationResult<ManagementGroupDeploymentResource>(
            OperationResult.Success, resource, null, null);
    }

    public OperationResult CancelDeployment(string groupId, string deploymentName)
    {
        logger.LogDebug(nameof(ManagementGroupDeploymentControlPlane), nameof(CancelDeployment),
            "Cancelling management-group-scope deployment '{0}' in management group '{1}'.",
            deploymentName, groupId);

        if (!provider.ManagementGroupExists(groupId))
            return OperationResult.NotFound;

        var deployment = provider.GetDeployment(groupId, deploymentName);
        if (deployment == null)
            return OperationResult.NotFound;

        return orchestrator.CancelDeployment(
            $"/providers/Microsoft.Management/managementGroups/{groupId}/providers/Microsoft.Resources/deployments/{deploymentName}");
    }

    public ControlPlaneOperationResult<DeploymentValidateResult> ValidateDeployment(
        string groupId,
        string deploymentName,
        CreateDeploymentRequest request)
    {
        if (!provider.ManagementGroupExists(groupId))
        {
            return new ControlPlaneOperationResult<DeploymentValidateResult>(
                OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId),
                NotFoundCode);
        }

        try
        {
            var template = request.ToTemplate();
            _templateEngineFacade.Validate(template);

            return new ControlPlaneOperationResult<DeploymentValidateResult>(OperationResult.Success,
                DeploymentValidateResult.FromRequestAtManagementGroupScope(groupId, deploymentName, request),
                null, null);
        }
        catch (Exception ex)
        {
            logger.LogDebug(nameof(ManagementGroupDeploymentControlPlane), nameof(ValidateDeployment),
                "Template validation failed: {0}", ex.Message);

            return new ControlPlaneOperationResult<DeploymentValidateResult>(OperationResult.Failed,
                null, ex.Message, "InvalidTemplate");
        }
    }

    public ControlPlaneOperationResult<ManagementGroupDeploymentResource> CreateOrUpdate(
        string groupId,
        string deploymentName,
        string content,
        Dictionary<string, CreateDeploymentRequest.ParameterValue>? parameters,
        string location,
        string mode)
    {
        logger.LogDebug(nameof(ManagementGroupDeploymentControlPlane), nameof(CreateOrUpdate),
            "Creating or updating management-group-scope deployment '{0}' in management group '{1}'.",
            deploymentName, groupId);

        if (!provider.ManagementGroupExists(groupId))
        {
            return new ControlPlaneOperationResult<ManagementGroupDeploymentResource>(
                OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId),
                NotFoundCode);
        }

        var template = _templateEngineFacade.Parse(content);
        var deploymentResource = new ManagementGroupDeploymentResource(
            groupId,
            deploymentName,
            location,
            DeploymentResourceProperties.New(mode, content, parameters));

        provider.CreateOrUpdateDeployment(groupId, deploymentName, deploymentResource);

        orchestrator.EnqueueManagementGroupDeployment(groupId, template, deploymentResource,
            InsensitiveDictionary<JToken>.Empty);

        return new ControlPlaneOperationResult<ManagementGroupDeploymentResource>(
            OperationResult.Success, deploymentResource, null, null);
    }
}
