using System.Xml;
using JetBrains.Annotations;
using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

public sealed class ServiceBusQueueResourceProperties
{
    public object? CountDetails { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public DateTimeOffset? AccessedOn { get; set; } = DateTimeOffset.UtcNow;
    public long? SizeInBytes { get; set; } = 0;
    public long? MessageCount { get; set; } = 0;
    public TimeSpan? LockDuration { get; set; }
    public int? MaxSizeInMegabytes { get; set; }
    public long? MaxMessageSizeInKilobytes { get; set; } = 0;
    public bool? RequiresDuplicateDetection { get; set; }
    public bool? RequiresSession { get; set; }
    public TimeSpan? DefaultMessageTimeToLive { get; set; } = TimeSpan.MaxValue;
    public bool? DeadLetteringOnMessageExpiration { get; set; }

    /// <summary>
    /// Implicitly this field is considered to be a TimeSpan, but the expected representation
    /// needs to be a duration object.
    /// </summary>
    public string? DuplicateDetectionHistoryTimeWindow
    {
        [UsedImplicitly] get => _duplicateDetectionHistoryTimeWindow.HasValue ? XmlConvert.ToString(_duplicateDetectionHistoryTimeWindow.Value) : null;
        set => _duplicateDetectionHistoryTimeWindow = string.IsNullOrWhiteSpace(value) ? null : XmlConvert.ToTimeSpan(value);
    }
    
    private TimeSpan? _duplicateDetectionHistoryTimeWindow;
    
    public int? MaxDeliveryCount { get; set; }
    public string? Status { get; set; }
    public bool? EnableBatchedOperations { get; set; }
    public TimeSpan? AutoDeleteOnIdle { get; set; } = TimeSpan.MaxValue;
    public bool? EnablePartitioning { get; set; }
    public bool? EnableExpress { get; set; }
    public string? ForwardTo { get; set; }
    public string? ForwardDeadLetteredMessagesTo { get; set; }
    public static ServiceBusQueueResourceProperties From(CreateOrUpdateServiceBusQueueRequest request)
    {
        var properties = request.Properties;
        return new ServiceBusQueueResourceProperties
        {
            CountDetails = properties?.CountDetails,
            LockDuration = properties?.LockDuration,
            MaxSizeInMegabytes = properties?.MaxSizeInMegabytes,
            MaxMessageSizeInKilobytes = properties?.MaxMessageSizeInKilobytes ?? 0,
            RequiresDuplicateDetection = properties?.RequiresDuplicateDetection,
            RequiresSession = properties?.RequiresSession,
            DeadLetteringOnMessageExpiration = properties?.DeadLetteringOnMessageExpiration,
            DuplicateDetectionHistoryTimeWindow = properties?.DuplicateDetectionHistoryTimeWindow.ToString() ?? XmlConvert.ToString(TimeSpan.FromMinutes(10)),
            ForwardTo = properties?.ForwardTo,
            ForwardDeadLetteredMessagesTo = properties?.ForwardDeadLetteredMessagesTo,
            DefaultMessageTimeToLive = properties?.DefaultMessageTimeToLive ?? TimeSpan.MaxValue,
            MaxDeliveryCount = properties?.MaxDeliveryCount,
            EnableBatchedOperations = properties?.EnableBatchedOperations,
            AutoDeleteOnIdle = properties?.AutoDeleteOnIdle ?? TimeSpan.MaxValue,
            EnablePartitioning = properties?.EnablePartitioning,
            EnableExpress = properties?.EnableExpress,
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
            properties.LockDuration = requestProps.LockDuration;
        
        if (requestProps.MaxSizeInMegabytes.HasValue)
            properties.MaxSizeInMegabytes = requestProps.MaxSizeInMegabytes;
        
        if (requestProps.MaxMessageSizeInKilobytes.HasValue)
            properties.MaxMessageSizeInKilobytes = requestProps.MaxMessageSizeInKilobytes;
        
        if (requestProps.RequiresDuplicateDetection.HasValue)
            properties.RequiresDuplicateDetection = requestProps.RequiresDuplicateDetection;
        
        if (requestProps.RequiresSession.HasValue)
            properties.RequiresSession = requestProps.RequiresSession;
        
        if (requestProps.DefaultMessageTimeToLive.HasValue)
            properties.DefaultMessageTimeToLive = requestProps.DefaultMessageTimeToLive;
        
        if (requestProps.DeadLetteringOnMessageExpiration.HasValue)
            properties.DeadLetteringOnMessageExpiration = requestProps.DeadLetteringOnMessageExpiration;
        
        if (requestProps.DuplicateDetectionHistoryTimeWindow.HasValue)
            properties.DuplicateDetectionHistoryTimeWindow = requestProps.DuplicateDetectionHistoryTimeWindow.ToString();
        
        if (requestProps.MaxDeliveryCount.HasValue)
            properties.MaxDeliveryCount = requestProps.MaxDeliveryCount;
        
        if (requestProps.EnableBatchedOperations.HasValue)
            properties.EnableBatchedOperations = requestProps.EnableBatchedOperations;
        
        if (requestProps.AutoDeleteOnIdle.HasValue)
            properties.AutoDeleteOnIdle = requestProps.AutoDeleteOnIdle;
        
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