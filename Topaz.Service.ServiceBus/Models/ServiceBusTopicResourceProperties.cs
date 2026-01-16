using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

public sealed class ServiceBusTopicResourceProperties
{
    public object? CountDetails { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? AccessedAt { get; set; } = DateTimeOffset.UtcNow;
    public long? SizeInBytes { get; set; } = 0;
    public TimeSpan? AutoDeleteOnIdle { get; set; }
    public TimeSpan? DefaultMessageTimeToLive { get; set; } = TimeSpan.FromDays(14);
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; } = TimeSpan.FromMinutes(10);
    public bool? EnableBatchedOperations { get; set; } = false;
    public bool? EnableExpress { get; set; } = false;
    public bool? EnablePartitioning { get; set; } = false;
    public long? MaxMessageSizeInKilobytes { get; set; } = 0;
    public int? MaxSizeInMegabytes { get; set; } = 0;
    public bool? RequiresDuplicateDetection { get; set; } = false;
    public string? Status { get; set; }
    public int? SubscriptionCount { get; set; } = 0;
    public bool? SupportOrdering { get; set; }

    public static ServiceBusTopicResourceProperties From(CreateOrUpdateServiceBusTopicRequest request)
    {
        var properties = request.Properties;
        return new ServiceBusTopicResourceProperties
        {
            CountDetails = properties?.CountDetails,
            AutoDeleteOnIdle = properties?.AutoDeleteOnIdle,
            DefaultMessageTimeToLive = properties?.DefaultMessageTimeToLive,
            DuplicateDetectionHistoryTimeWindow = properties?.DuplicateDetectionHistoryTimeWindow ?? TimeSpan.FromMinutes(10),
            EnableBatchedOperations = properties?.EnableBatchedOperations,
            EnableExpress = properties?.EnableExpress,
            EnablePartitioning = properties?.EnablePartitioning,
            MaxMessageSizeInKilobytes = properties?.MaxMessageSizeInKilobytes ?? 0,
            MaxSizeInMegabytes = properties?.MaxSizeInMegabytes,
            RequiresDuplicateDetection = properties?.RequiresDuplicateDetection,
            SupportOrdering = properties?.SupportOrdering,
            Status = properties?.Status
        };
    }
    
    internal static void UpdateFromRequest(ServiceBusTopicResource resource, CreateOrUpdateServiceBusTopicRequest request)
    {
        if (request.Properties == null)
        {
            throw new ArgumentNullException(nameof(request.Properties));
        }

        var properties = resource.Properties;
        var requestProps = request.Properties;

        if (requestProps.AutoDeleteOnIdle.HasValue)
            properties.AutoDeleteOnIdle = requestProps.AutoDeleteOnIdle;
        
        if (requestProps.DefaultMessageTimeToLive.HasValue)
            properties.DefaultMessageTimeToLive = requestProps.DefaultMessageTimeToLive;
        
        if (requestProps.DuplicateDetectionHistoryTimeWindow.HasValue)
            properties.DuplicateDetectionHistoryTimeWindow = requestProps.DuplicateDetectionHistoryTimeWindow;
        
        if (requestProps.EnableBatchedOperations.HasValue)
            properties.EnableBatchedOperations = requestProps.EnableBatchedOperations;
        
        if (requestProps.EnableExpress.HasValue)
            properties.EnableExpress = requestProps.EnableExpress;
        
        if (requestProps.EnablePartitioning.HasValue)
            properties.EnablePartitioning = requestProps.EnablePartitioning;
        
        if (requestProps.MaxMessageSizeInKilobytes.HasValue)
            properties.MaxMessageSizeInKilobytes = requestProps.MaxMessageSizeInKilobytes;
        
        if (requestProps.MaxSizeInMegabytes.HasValue)
            properties.MaxSizeInMegabytes = requestProps.MaxSizeInMegabytes;
        
        if (requestProps.RequiresDuplicateDetection.HasValue)
            properties.RequiresDuplicateDetection = requestProps.RequiresDuplicateDetection;
        
        if (requestProps.SupportOrdering.HasValue)
            properties.SupportOrdering = requestProps.SupportOrdering;
        
        if (requestProps.Status != null)
            properties.Status = requestProps.Status;
        
        if (requestProps.CountDetails != null)
            properties.CountDetails = requestProps.CountDetails;
        
        properties.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
