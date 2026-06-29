using System.Text.Json;
using Topaz.Service.ResourceManager.Models;
using Topaz.Shared;

namespace Topaz.Service.ResourceManager.Deployment;

/// <summary>
/// Reads and writes per-deployment operation records to an <c>operations.json</c>
/// file that lives alongside the deployment's <c>metadata.json</c>.
/// </summary>
internal static class OperationStore
{
    private const string FileName = "operations.json";

    // Uses ResourceManagerService.LocalDirectoryPath template, which is:
    // .subscription/{subscriptionId}/.resource-group/{resourceGroup}/.resource-manager
    internal static string GetRgScopeDirectory(string sub, string rg, string deploymentName) =>
        Path.Combine(
            GlobalSettings.MainEmulatorDirectory,
            ResourceManagerService.LocalDirectoryPath
                .Replace("{subscriptionId}", sub)
                .Replace("{resourceGroup}", rg),
            deploymentName);

    // Uses SubscriptionDeploymentService.LocalDirectoryPath template, which is:
    // .subscription/{subscriptionId}/.resource-manager
    internal static string GetSubScopeDirectory(string sub, string deploymentName) =>
        Path.Combine(
            GlobalSettings.MainEmulatorDirectory,
            SubscriptionDeploymentService.LocalDirectoryPath
                .Replace("{subscriptionId}", sub),
            deploymentName);

    // Management-group deployments live at:
    // .management-group/{groupId}/.resource-manager/{deploymentName}
    internal static string GetMgScopeDirectory(string groupId, string deploymentName) =>
        Path.Combine(
            GlobalSettings.MainEmulatorDirectory,
            ManagementGroupDeploymentService.LocalDirectoryPath,
            groupId,
            ".resource-manager",
            deploymentName);

    internal static void Append(string dirPath, OperationRecord record)
    {
        var file = Path.Combine(dirPath, FileName);
        var existing = ReadAll(file);
        existing.Add(record);
        File.WriteAllText(file, JsonSerializer.Serialize(existing, GlobalSettings.JsonOptions));
    }

    internal static IReadOnlyList<OperationRecord> GetAll(string dirPath)
    {
        var file = Path.Combine(dirPath, FileName);
        return ReadAll(file);
    }

    private static List<OperationRecord> ReadAll(string file)
    {
        if (!File.Exists(file)) return [];
        var json = File.ReadAllText(file);
        return JsonSerializer.Deserialize<List<OperationRecord>>(json, GlobalSettings.JsonOptions) ?? [];
    }
}
