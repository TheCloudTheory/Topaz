using System.Xml;
using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

public sealed class ServiceBusQueueResourceProperties : ServiceBusEntityResourceProperties
{
    public static ServiceBusQueueResourceProperties From(CreateOrUpdateServiceBusQueueRequest request)
    {
        var properties = request.Properties;
        return new ServiceBusQueueResourceProperties
        {
            CountDetails = properties?.CountDetails,
            LockDuration = DeterminePropertyValue(properties?.LockDuration, TimeSpan.FromSeconds(60)),
            MaxSizeInMegabytes = properties?.MaxSizeInMegabytes ?? 1024,
            MaxMessageSizeInKilobytes = properties?.MaxMessageSizeInKilobytes ?? 0,
            RequiresDuplicateDetection = properties?.RequiresDuplicateDetection ?? false,
            RequiresSession = properties?.RequiresSession ?? false,
            DeadLetteringOnMessageExpiration = properties?.DeadLetteringOnMessageExpiration ?? false,
            DuplicateDetectionHistoryTimeWindow = DeterminePropertyValue(properties?.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMinutes(10)),
            ForwardTo = properties?.ForwardTo,
            ForwardDeadLetteredMessagesTo = properties?.ForwardDeadLetteredMessagesTo,
            DefaultMessageTimeToLive = DeterminePropertyValue(properties?.DefaultMessageTimeToLive, TimeSpan.MaxValue),
            MaxDeliveryCount = properties?.MaxDeliveryCount ?? 10,
            EnableBatchedOperations = properties?.EnableBatchedOperations ?? false,
            AutoDeleteOnIdle = DeterminePropertyValue(properties?.AutoDeleteOnIdle, TimeSpan.MaxValue),
            EnablePartitioning = properties?.EnablePartitioning ?? false,
            EnableExpress = properties?.EnableExpress ?? false,
            Status = properties?.Status,
        };
    }

    internal static void UpdateFromRequest(ServiceBusQueueResource existingQueue, CreateOrUpdateServiceBusQueueRequest request)
    {
        if (request.Properties == null)
        {
            throw new ArgumentNullException(nameof(request.Properties));
        }

        var properties = existingQueue.Properties;
        var requestProps = request.Properties;

        if (requestProps.LockDuration.HasValue)
            properties.LockDuration = requestProps.LockDuration.ToString();
        
        if (requestProps.MaxSizeInMegabytes.HasValue)
            properties.MaxSizeInMegabytes = requestProps.MaxSizeInMegabytes;
        
        if (requestProps.MaxMessageSizeInKilobytes.HasValue)
            properties.MaxMessageSizeInKilobytes = requestProps.MaxMessageSizeInKilobytes;
        
        if (requestProps.RequiresDuplicateDetection.HasValue)
            properties.RequiresDuplicateDetection = requestProps.RequiresDuplicateDetection;
        
        if (requestProps.RequiresSession.HasValue)
            properties.RequiresSession = requestProps.RequiresSession;
        
        if (requestProps.DefaultMessageTimeToLive.HasValue)
            properties.DefaultMessageTimeToLive = requestProps.DefaultMessageTimeToLive.ToString();
        
        if (requestProps.DeadLetteringOnMessageExpiration.HasValue)
            properties.DeadLetteringOnMessageExpiration = requestProps.DeadLetteringOnMessageExpiration;
        
        if (requestProps.DuplicateDetectionHistoryTimeWindow.HasValue)
            properties.DuplicateDetectionHistoryTimeWindow = requestProps.DuplicateDetectionHistoryTimeWindow.ToString();
        
        if (requestProps.MaxDeliveryCount.HasValue)
            properties.MaxDeliveryCount = requestProps.MaxDeliveryCount;
        
        if (requestProps.EnableBatchedOperations.HasValue)
            properties.EnableBatchedOperations = requestProps.EnableBatchedOperations;
        
        if (requestProps.AutoDeleteOnIdle.HasValue)
            properties.AutoDeleteOnIdle = requestProps.AutoDeleteOnIdle.ToString();
        
        if (requestProps.EnablePartitioning.HasValue)
            properties.EnablePartitioning = requestProps.EnablePartitioning;
        
        if (requestProps.EnableExpress.HasValue)
            properties.EnableExpress = requestProps.EnableExpress;
        
        if (requestProps.ForwardTo != null)
            properties.ForwardTo = requestProps.ForwardTo;
        
        if (requestProps.ForwardDeadLetteredMessagesTo != null)
            properties.ForwardDeadLetteredMessagesTo = requestProps.ForwardDeadLetteredMessagesTo;
        
        if (requestProps.CountDetails != null)
            properties.CountDetails = requestProps.CountDetails;
        
        if (requestProps.SizeInBytes.HasValue)
            properties.SizeInBytes = requestProps.SizeInBytes;
        
        if (requestProps.MessageCount.HasValue)
            properties.MessageCount = requestProps.MessageCount;
        
        properties.UpdatedOn = DateTimeOffset.UtcNow;
    }
}