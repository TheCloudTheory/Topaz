using System.Text.Json;
using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry.Models.Requests;

namespace Topaz.Service.ContainerRegistry.Models;

[UsedImplicitly]
internal sealed class AcrRunResourceProperties
{
    public string RunId { get; set; } = string.Empty;
    public string Status { get; set; } = "Succeeded";
    public string ProvisioningState { get; set; } = "Succeeded";
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset FinishTime { get; set; }
    public string RunType { get; set; } = "QuickRun";
    public string? Task { get; set; }
    public bool IsArchiveEnabled { get; set; }
    public JsonElement? Platform { get; set; }
    public JsonElement? AgentConfiguration { get; set; }

    public static AcrRunResourceProperties CreateQueued(string runId, string? taskName, string runType)
    {
        var now = DateTimeOffset.UtcNow;
        return new AcrRunResourceProperties
        {
            RunId = runId,
            Status = "Queued",
            ProvisioningState = "Creating",
            CreateTime = now,
            StartTime = now,
            RunType = runType,
            Task = taskName,
            IsArchiveEnabled = false
        };
    }

    public static AcrRunResourceProperties FromTaskRun(
        string taskName,
        string runId,
        RunAcrTaskRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        return new AcrRunResourceProperties
        {
            RunId = runId,
            Status = "Succeeded",
            ProvisioningState = "Succeeded",
            CreateTime = now,
            StartTime = now,
            FinishTime = now,
            RunType = "AutoRun",
            Task = taskName,
            IsArchiveEnabled = request.Properties?.IsArchiveEnabled ?? false
        };
    }

    public static AcrRunResourceProperties FromScheduleRun(
        string runId,
        ScheduleAcrRunRequest request)
    {
        var now = DateTimeOffset.UtcNow;
        var runType = request.Type switch
        {
            "DockerBuildRequest" => "QuickBuild",
            "TaskRunRequest" => "QuickRun",
            "FileTaskRunRequest" => "QuickRun",
            "EncodedTaskRunRequest" => "QuickRun",
            _ => "QuickRun"
        };
        return new AcrRunResourceProperties
        {
            RunId = runId,
            Status = "Succeeded",
            ProvisioningState = "Succeeded",
            CreateTime = now,
            StartTime = now,
            FinishTime = now,
            RunType = runType,
            IsArchiveEnabled = false
        };
    }

    public static void UpdateFromRequest(AcrRunResource resource, UpdateAcrRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (request.IsArchiveEnabled.HasValue)
            resource.Properties.IsArchiveEnabled = request.IsArchiveEnabled.Value;
    }
}
