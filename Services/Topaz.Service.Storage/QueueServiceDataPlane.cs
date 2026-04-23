using System.Text.Json;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage;

internal sealed class QueueServiceDataPlane(QueueServiceControlPlane controlPlane, ITopazLogger logger)
{
    public DataPlaneOperationResult<QueueEnumerationResult> ListQueues(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(ListQueues),
            "Executing {0}: {1}", nameof(ListQueues), storageAccountName);

        var result = controlPlane.ListQueues(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName);
        if (result.Result == OperationResult.Success && result.Resource != null)
        {
            var queueEnumeration = new QueueEnumerationResult(storageAccountName, result.Resource);
            return new DataPlaneOperationResult<QueueEnumerationResult>(OperationResult.Success, queueEnumeration,
                null, null);
        }

        return new DataPlaneOperationResult<QueueEnumerationResult>(OperationResult.Failed, null,
            "Failed to list queues.", null);
    }

    public DataPlaneOperationResult<Queue> CreateQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(CreateQueue),
            "Executing {0}: {1} {2}", nameof(CreateQueue), storageAccountName, queueName);

        if (controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
                queueName))
        {
            return new DataPlaneOperationResult<Queue>(OperationResult.Conflict, null,
                "Queue already exists.", "QueueAlreadyExists");
        }

        var result = controlPlane.CreateQueue(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        return new DataPlaneOperationResult<Queue>(result.Result, result.Resource, result.Reason,
            result.Code);
    }

    public DataPlaneOperationResult DeleteQueue(SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, string storageAccountName, string queueName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(DeleteQueue),
            "Executing {0}: {1} {2}", nameof(DeleteQueue), storageAccountName, queueName);

        if (!controlPlane.QueueExists(subscriptionIdentifier, resourceGroupIdentifier, storageAccountName,
                queueName))
        {
            return new DataPlaneOperationResult(OperationResult.NotFound, "Queue not found.", "QueueNotFound");
        }

        var result = controlPlane.DeleteQueue(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        return new DataPlaneOperationResult(result.Result, result.Reason, result.Code);
    }

    public DataPlaneOperationResult<QueueProperties> GetQueueProperties(
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        string storageAccountName, string queueName)
    {
        logger.LogDebug(nameof(QueueServiceDataPlane), nameof(GetQueueProperties),
            "Executing {0}: {1} {2}", nameof(GetQueueProperties), storageAccountName, queueName);

        var result = controlPlane.GetQueueProperties(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccountName, queueName);

        return new DataPlaneOperationResult<QueueProperties>(result.Result, result.Resource, result.Reason,
            result.Code);
    }
}

public sealed class QueueEnumerationResult
{
    public string? StorageAccountName { get; set; }
    public QueueProperties[]? Queues { get; set; }

    public QueueEnumerationResult(string storageAccountName, QueueProperties[] queues)
    {
        StorageAccountName = storageAccountName;
        Queues = queues;
    }
}
