using System.Text.Json;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Shared.Models;

namespace Topaz.Service.ContainerRegistry;

internal sealed partial class ContainerRegistryControlPlane
{
    private const string TasksSubresource = "tasks";
    private const string RunsSubresource = "runs";

    private const string TaskNotFoundCode = "TaskNotFound";
    private const string TaskNotFoundMessageTemplate = "ACR task '{0}' could not be found in registry '{1}'";

    private const string RunNotFoundCode = "RunNotFound";
    private const string RunNotFoundMessageTemplate = "ACR run '{0}' could not be found in registry '{1}'";

    public ControlPlaneOperationResult<AcrTaskResource> CreateOrUpdateTask(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName,
        CreateOrUpdateAcrTaskRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(CreateOrUpdateTask),
            "Executing {0}: registry={1}, task={2}", nameof(CreateOrUpdateTask), registryName, taskName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrTaskResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var existing = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        AcrTaskResource resource;
        if (existing == null)
        {
            var location = request.Location ?? registryOperation.Resource!.Location ?? "eastus";
            var properties = AcrTaskResourceProperties.FromRequest(request);
            resource = new AcrTaskResource(
                subscriptionIdentifier, resourceGroupIdentifier, registryName, taskName,
                location, request.Tags, request.Identity, properties);
        }
        else
        {
            AcrTaskResourceProperties.UpdateFromRequest(existing, new UpdateAcrTaskRequest
            {
                Tags = request.Tags ?? existing.Tags,
                Identity = request.Identity ?? existing.Identity,
                Properties = request.Properties == null ? null : new UpdateAcrTaskRequest.UpdateAcrTaskRequestProperties
                {
                    Status = request.Properties.Status,
                    Timeout = request.Properties.Timeout,
                    Platform = request.Properties.Platform,
                    AgentConfiguration = request.Properties.AgentConfiguration,
                    Step = request.Properties.Step,
                    Trigger = request.Properties.Trigger,
                    Credentials = request.Properties.Credentials
                }
            });
            resource = existing;
        }

        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource, resource);

        var result = existing == null ? OperationResult.Created : OperationResult.Updated;
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(CreateOrUpdateTask),
            "Executing {0}: task '{1}' {2}.", nameof(CreateOrUpdateTask), taskName,
            existing == null ? "created" : "updated");
        return new ControlPlaneOperationResult<AcrTaskResource>(result, resource);
    }

    public ControlPlaneOperationResult<AcrTaskResource> GetTask(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(GetTask),
            "Executing {0}: registry={1}, task={2}", nameof(GetTask), registryName, taskName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrTaskResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var resource = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        return resource == null
            ? new ControlPlaneOperationResult<AcrTaskResource>(
                OperationResult.NotFound, null,
                string.Format(TaskNotFoundMessageTemplate, taskName, registryName),
                TaskNotFoundCode)
            : new ControlPlaneOperationResult<AcrTaskResource>(OperationResult.Success, resource);
    }

    public ControlPlaneOperationResult DeleteTask(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(DeleteTask),
            "Executing {0}: registry={1}, task={2}", nameof(DeleteTask), registryName, taskName);

        var existing = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        if (existing == null)
            return new ControlPlaneOperationResult(
                OperationResult.NotFound,
                string.Format(TaskNotFoundMessageTemplate, taskName, registryName),
                TaskNotFoundCode);

        provider.DeleteSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(DeleteTask),
            "Executing {0}: task '{1}' deleted.", nameof(DeleteTask), taskName);
        return new ControlPlaneOperationResult(OperationResult.Deleted);
    }

    public ControlPlaneOperationResult<AcrTaskResource[]> ListTasks(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListTasks),
            "Executing {0}: registry={1}", nameof(ListTasks), registryName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrTaskResource[]>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var tasks = provider.ListSubresourcesAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, TasksSubresource);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListTasks),
            "Executing {0}: Found {1} tasks.", nameof(ListTasks), tasks.Length);
        return new ControlPlaneOperationResult<AcrTaskResource[]>(OperationResult.Success, tasks);
    }

    public ControlPlaneOperationResult<AcrTaskResource> UpdateTask(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName,
        UpdateAcrTaskRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(UpdateTask),
            "Executing {0}: registry={1}, task={2}", nameof(UpdateTask), registryName, taskName);

        var existing = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        if (existing == null)
            return new ControlPlaneOperationResult<AcrTaskResource>(
                OperationResult.NotFound, null,
                string.Format(TaskNotFoundMessageTemplate, taskName, registryName),
                TaskNotFoundCode);

        AcrTaskResourceProperties.UpdateFromRequest(existing, request);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource, existing);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(UpdateTask),
            "Executing {0}: task '{1}' updated.", nameof(UpdateTask), taskName);
        return new ControlPlaneOperationResult<AcrTaskResource>(OperationResult.Updated, existing);
    }

    public ControlPlaneOperationResult<AcrRunResource> TriggerTaskRun(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string taskName,
        RunAcrTaskRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(TriggerTaskRun),
            "Executing {0}: registry={1}, task={2}", nameof(TriggerTaskRun), registryName, taskName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);
        }

        var taskResource = provider.GetSubresourceAs<AcrTaskResource>(
            subscriptionIdentifier, resourceGroupIdentifier, taskName, registryName, TasksSubresource);

        if (taskResource == null)
        {
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null,
                string.Format(TaskNotFoundMessageTemplate, taskName, registryName),
                TaskNotFoundCode);
        }

        var runId = Guid.NewGuid().ToString("N")[..8];

        switch (taskResource.Properties.Step)
        {
            case AcrTaskDockerBuildStep dockerStep when
                AcrDockerExecutor.IsAvailable():
            {
                var buildResult = RunDockerBuildRequest(subscriptionIdentifier, resourceGroupIdentifier,
                    registryName, taskName, dockerStep, runId);

                logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(TriggerTaskRun),
                    "Executing {0}: Docker run '{1}' queued for task '{2}'.", nameof(TriggerTaskRun), runId, taskName);
                return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Created, buildResult);
            }
            case AcrTaskFileTaskStep fileTaskStep:
            {
                var buildResult = RunFileTaskRequest(subscriptionIdentifier, resourceGroupIdentifier,
                    registryName, taskName, fileTaskStep, runId);

                logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(TriggerTaskRun),
                    "Executing {0}: File run '{1}' queued for task '{2}'.", nameof(TriggerTaskRun), runId, taskName);
                return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Created, buildResult);
            }
        }

        // Non-Docker step or Docker unavailable: immediate-Succeeded.
        var properties = AcrRunResourceProperties.FromTaskRun(taskName, runId, request);
        var resource = new AcrRunResource(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, properties);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(TriggerTaskRun),
            "Executing {0}: run '{1}' created for task '{2}'.", nameof(TriggerTaskRun), runId, taskName);

        return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Created, resource);

    }

    private AcrRunResource RunFileTaskRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string registryName, string taskName,
        AcrTaskFileTaskStep fileTaskStep, string runId)
    {
        var properties = AcrRunResourceProperties.CreateQueued(runId, taskName, "AutoRun");
        var resource = new AcrRunResource(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, properties);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);

        _ = ExecuteRunAsync(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId,
            fileTaskStep);

        return resource;
    }

    private async Task ExecuteRunAsync(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string registryName, string runId,
        AcrTaskFileTaskStep fileTaskStep)
    {
        var logPath = provider.GetRunLogPath(runId);
        try
        {
            // Transition to Running
            var resource = provider.GetSubresourceAs<AcrRunResource>(
                subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);
            if (resource != null)
            {
                resource.Properties.Status = "Running";
                resource.Properties.ProvisioningState = "Running";
                resource.Properties.StartTime = DateTimeOffset.UtcNow;
                provider.CreateOrUpdateSubresource(
                    subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
            }

            var success = true;

            // Transition to Succeeded or Failed
            resource = provider.GetSubresourceAs<AcrRunResource>(
                subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);
            if (resource != null)
            {
                resource.Properties.Status = success ? "Succeeded" : "Failed";
                resource.Properties.ProvisioningState = success ? "Succeeded" : "Failed";
                resource.Properties.FinishTime = DateTimeOffset.UtcNow;
                provider.CreateOrUpdateSubresource(
                    subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            try
            {
                await File.AppendAllTextAsync(logPath, $"Fatal error: {ex.Message}{Environment.NewLine}");
                var resource = provider.GetSubresourceAs<AcrRunResource>(
                    subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);
                if (resource != null)
                {
                    resource.Properties.Status = "Failed";
                    resource.Properties.ProvisioningState = "Failed";
                    resource.Properties.FinishTime = DateTimeOffset.UtcNow;
                    provider.CreateOrUpdateSubresource(
                        subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
                }
            }
            catch { /* best effort */ }
        }
    }

    private AcrRunResource RunDockerBuildRequest(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string registryName, string taskName,
        AcrTaskDockerBuildStep dockerStep, string runId)
    {
        var contextPath = dockerStep.ContextPath ?? ".";
        var dockerFilePath = dockerStep.DockerFilePath ?? "Dockerfile";
        var imageNames = dockerStep.ImageNames ?? [];
        var imageName = imageNames.Count > 0 ? imageNames[0] : registryName.ToLowerInvariant() + ":latest";
        var isPushEnabled = dockerStep.IsPushEnabled == true;

        var properties = AcrRunResourceProperties.CreateQueued(runId, taskName, "AutoRun");
        var resource = new AcrRunResource(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, properties);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);

        _ = ExecuteRunAsync(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId,
            contextPath, dockerFilePath, imageName, isPushEnabled);

        return resource;
    }

    public ControlPlaneOperationResult<AcrRunResource> ScheduleRun(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string rawRequest)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ScheduleRun),
            "Executing {0}: registry={1}", nameof(ScheduleRun), registryName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
        {
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);
        }

        var request = TopazApiRequest.Deserialize<ScheduleAcrRunRequest>(rawRequest)!;
        var runId = Guid.NewGuid().ToString("N")[..8];
        var type = request.Type;

        if (string.IsNullOrWhiteSpace(type))
        {
            return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.BadRequest, null, "Type of the request is missing.", "BadRequest");
        }
        
        if (string.Equals(type, "DockerBuildRequest", StringComparison.OrdinalIgnoreCase) &&
            AcrDockerExecutor.IsAvailable())
        {
            var result = ExecuteScheduledDockerRun(subscriptionIdentifier, resourceGroupIdentifier, registryName, rawRequest, runId);

            logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ScheduleRun),
                "Executing {0}: Docker run '{1}' queued.", nameof(ScheduleRun), runId);
            
            return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Created, result);
        }

        if (string.Equals(type, "FileTaskRunRequest"))
        {
            var taskRunRequest = TopazApiRequest.Deserialize<ScheduleTaskRunRequest>(rawRequest)!;
            var validation = taskRunRequest.Validate<ScheduleTaskRunRequest>();
            if (!validation.IsValid)
            {
                return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.BadRequest, null,
                    validation.Error, "BadRequest");
            }

            var result = ExecuteScheduledTaskRun(subscriptionIdentifier, resourceGroupIdentifier,
                registryName, taskRunRequest, runId);

            logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ScheduleRun),
                "Executing {0}: Docker run '{1}' queued.", nameof(ScheduleRun), runId);
            
            return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Created, result);
        }

        // Non-DockerBuildRequest or Docker unavailable: immediate-Succeeded.
        var properties = AcrRunResourceProperties.FromScheduleRun(runId, request);
        var resource = new AcrRunResource(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, properties);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ScheduleRun),
            "Executing {0}: run '{1}' created.", nameof(ScheduleRun), runId);
        return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Created, resource);
        
    }

    private AcrRunResource ExecuteScheduledTaskRun(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string registryName,
        ScheduleTaskRunRequest request, string runId)
    {
        var properties = AcrRunResourceProperties.FromScheduleTaskRun(runId, request);
        var resource = new AcrRunResource(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, properties);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
        
        return resource;
    }

    private AcrRunResource ExecuteScheduledDockerRun(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string registryName, string rawRequest, string runId)
    {
        var request = TopazApiRequest.Deserialize<ScheduleAcrRunRequest>(rawRequest)!;
        var contextPath = request.ContextPath ?? ".";
        var dockerFilePath = request.DockerFilePath ?? "Dockerfile";
        var imageName = request.ImageNames?.Length > 0
            ? request.ImageNames[0]
            : registryName.ToLowerInvariant() + ":latest";

        var properties = AcrRunResourceProperties.CreateQueued(runId, null, "QuickBuild");
        var resource = new AcrRunResource(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId, properties);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);

        _ = ExecuteRunAsync(subscriptionIdentifier, resourceGroupIdentifier, registryName, runId,
            contextPath, dockerFilePath, imageName, request.IsPushEnabled);
        
        return resource;
    }

    public string? GetRunLog(string runId) => provider.ReadRunLog(runId);

    private async Task ExecuteRunAsync(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string runId,
        string contextPath,
        string dockerFilePath,
        string imageName,
        bool isPushEnabled)
    {
        var logPath = provider.GetRunLogPath(runId);
        try
        {
            // Transition to Running
            var resource = provider.GetSubresourceAs<AcrRunResource>(
                subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);
            if (resource != null)
            {
                resource.Properties.Status = "Running";
                resource.Properties.ProvisioningState = "Running";
                resource.Properties.StartTime = DateTimeOffset.UtcNow;
                provider.CreateOrUpdateSubresource(
                    subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
            }

            var success = await AcrDockerExecutor.ExecuteAsync(
                contextPath, dockerFilePath, imageName, isPushEnabled, logPath, CancellationToken.None);

            // Transition to Succeeded or Failed
            resource = provider.GetSubresourceAs<AcrRunResource>(
                subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);
            if (resource != null)
            {
                resource.Properties.Status = success ? "Succeeded" : "Failed";
                resource.Properties.ProvisioningState = success ? "Succeeded" : "Failed";
                resource.Properties.FinishTime = DateTimeOffset.UtcNow;
                provider.CreateOrUpdateSubresource(
                    subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            try
            {
                await File.AppendAllTextAsync(logPath, $"Fatal error: {ex.Message}{Environment.NewLine}");
                var resource = provider.GetSubresourceAs<AcrRunResource>(
                    subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);
                if (resource != null)
                {
                    resource.Properties.Status = "Failed";
                    resource.Properties.ProvisioningState = "Failed";
                    resource.Properties.FinishTime = DateTimeOffset.UtcNow;
                    provider.CreateOrUpdateSubresource(
                        subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, resource);
                }
            }
            catch { /* best effort */ }
        }
    }

    public ControlPlaneOperationResult<AcrRunResource> GetRun(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string runId)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(GetRun),
            "Executing {0}: registry={1}, run={2}", nameof(GetRun), registryName, runId);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var resource = provider.GetSubresourceAs<AcrRunResource>(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);

        return resource == null
            ? new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null,
                string.Format(RunNotFoundMessageTemplate, runId, registryName),
                RunNotFoundCode)
            : new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Success, resource);
    }

    public ControlPlaneOperationResult<AcrRunResource[]> ListRuns(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListRuns),
            "Executing {0}: registry={1}", nameof(ListRuns), registryName);

        var registryOperation = Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (registryOperation.Result == OperationResult.NotFound)
            return new ControlPlaneOperationResult<AcrRunResource[]>(
                OperationResult.NotFound, null, registryOperation.Reason, registryOperation.Code);

        var runs = provider.ListSubresourcesAs<AcrRunResource>(
            subscriptionIdentifier, resourceGroupIdentifier, registryName, RunsSubresource);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(ListRuns),
            "Executing {0}: Found {1} runs.", nameof(ListRuns), runs.Length);
        return new ControlPlaneOperationResult<AcrRunResource[]>(OperationResult.Success, runs);
    }

    public ControlPlaneOperationResult<AcrRunResource> UpdateRun(
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName,
        string runId,
        UpdateAcrRunRequest request)
    {
        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(UpdateRun),
            "Executing {0}: registry={1}, run={2}", nameof(UpdateRun), registryName, runId);

        var existing = provider.GetSubresourceAs<AcrRunResource>(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource);

        if (existing == null)
            return new ControlPlaneOperationResult<AcrRunResource>(
                OperationResult.NotFound, null,
                string.Format(RunNotFoundMessageTemplate, runId, registryName),
                RunNotFoundCode);

        AcrRunResourceProperties.UpdateFromRequest(existing, request);
        provider.CreateOrUpdateSubresource(
            subscriptionIdentifier, resourceGroupIdentifier, runId, registryName, RunsSubresource, existing);

        logger.LogDebug(nameof(ContainerRegistryControlPlane), nameof(UpdateRun),
            "Executing {0}: run '{1}' updated.", nameof(UpdateRun), runId);
        return new ControlPlaneOperationResult<AcrRunResource>(OperationResult.Updated, existing);
    }
}