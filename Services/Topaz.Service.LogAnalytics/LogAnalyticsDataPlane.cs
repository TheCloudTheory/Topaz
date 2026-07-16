using Topaz.EventPipeline;
using Topaz.Service.LogAnalytics.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics;

internal sealed class LogAnalyticsDataPlane(
    WorkspaceResourceProvider provider,
    LogAnalyticsServiceControlPlane controlPlane,
    SubscriptionControlPlane subscriptionControlPlane,
    ITopazLogger logger)
{
    public static LogAnalyticsDataPlane New(Pipeline eventPipeline, ITopazLogger logger) => new(
        new WorkspaceResourceProvider(logger), LogAnalyticsServiceControlPlane.New(eventPipeline, logger),
        SubscriptionControlPlane.New(eventPipeline, logger), logger);

    public DataPlaneOperationResult SaveIngestedData(string workspaceId, string? logTypeValue, string data)
    {
        var subscriptions = subscriptionControlPlane.List();
        if (subscriptions.Result != OperationResult.Success || subscriptions.Resource == null ||
            subscriptions.Resource.Length == 0)
        {
            return new DataPlaneOperationResult(OperationResult.Failed, "No subscriptions found",
                "NoSubscriptionsFound");
        }

        var workspaces = new List<WorkspaceResource>();
        foreach (var subscription in subscriptions.Resource)
        {
            var workspacesInSubscription =
                controlPlane.ListBySubscription(SubscriptionIdentifier.From(subscription.SubscriptionId));
            if (workspacesInSubscription.Result != OperationResult.Success ||
                workspacesInSubscription.Resource == null || workspacesInSubscription.Resource.Length == 0)
            {
                continue;
            }

            workspaces.AddRange(workspacesInSubscription.Resource);
        }

        if (workspaces.Count == 0 || workspaces.All(w => w.Properties.WorkspaceId != workspaceId))
        {
            return new DataPlaneOperationResult(OperationResult.Failed, "No workspaces found", "NoWorkspacesFound");
        }

        var workspace = workspaces.SingleOrDefault(w => w.Properties.WorkspaceId == workspaceId)!;
        var rawTableName = logTypeValue ?? "CustomLog";
        var tableName = rawTableName.EndsWith("_CL", StringComparison.OrdinalIgnoreCase)
            ? rawTableName
            : rawTableName + "_CL";
        var dir = provider.GetDataPath(tableName, DateTime.UtcNow, Guid.NewGuid().ToString());

        logger.LogDebug(nameof(LogAnalyticsDataPlane), nameof(SaveIngestedData),
            $"Workspace: {workspace.Id}, Log Type: {tableName}");

        provider.SaveIngestedData(workspace.GetSubscription(), workspace.GetResourceGroup(), workspace.Name, data, dir);

        return new DataPlaneOperationResult(OperationResult.Success, null, null);
    }

    public DataPlaneOperationResult<LogAnalyticsQueryResult> QueryData(string? workspaceId, string query)
    {
        var subscriptions = subscriptionControlPlane.List();
        if (subscriptions.Result != OperationResult.Success || subscriptions.Resource == null ||
            subscriptions.Resource.Length == 0)
            return new DataPlaneOperationResult<LogAnalyticsQueryResult>(
                OperationResult.Failed, null, "No subscriptions found", "NoSubscriptionsFound");

        WorkspaceResource? workspace = null;
        foreach (var subscription in subscriptions.Resource)
        {
            var ws = controlPlane.ListBySubscription(SubscriptionIdentifier.From(subscription.SubscriptionId));
            if (ws.Result != OperationResult.Success || ws.Resource == null) continue;
            workspace = ws.Resource.FirstOrDefault(w => w.Properties.WorkspaceId == workspaceId);
            if (workspace != null) break;
        }

        if (workspace == null)
            return new DataPlaneOperationResult<LogAnalyticsQueryResult>(
                OperationResult.NotFound, null, "Workspace not found", "WorkspaceNotFound");

        var sub = workspace.GetSubscription();
        var rg = workspace.GetResourceGroup();
        var name = workspace.Name;

        var result = KqlQueryExecutor.Execute(
            query,
            tableName => provider.LoadIngestedData(sub, rg, name, tableName));

        return new DataPlaneOperationResult<LogAnalyticsQueryResult>(
            OperationResult.Success, result, null, null);
    }
}