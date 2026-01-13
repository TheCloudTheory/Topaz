using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

internal sealed class ServiceBusTopicResourceProperties
{
    public object? CountDetails { get; init; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? AccessedAt { get; init; }
    public long? SizeInBytes { get; init; }
    public TimeSpan? AutoDeleteOnIdle { get; init; }
    public TimeSpan? DefaultMessageTimeToLive { get; init; }
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; init; }
    public bool? EnableBatchedOperations { get; init; }
    public bool? EnableExpress { get; init; }
    public bool? EnablePartitioning { get; init; }
    public long? MaxMessageSizeInKilobytes { get; init; }
    public int? MaxSizeInMegabytes { get; init; }
    public bool? RequiresDuplicateDetection { get; init; }
    public int? Status { get; init; }
    public int? SubscriptionCount { get; init; }
    public bool? SupportOrdering { get; init; }

    public static ServiceBusTopicResourceProperties From(CreateOrUpdateServiceBusTopicRequest request)
    {
        return new ServiceBusTopicResourceProperties
        {
            CountDetails = request.Properties.CountDetails,
            SizeInBytes = request.Properties.SizeInBytes,
            AutoDeleteOnIdle = request.Properties.AutoDeleteOnIdle,
            DefaultMessageTimeToLive = request.Properties.DefaultMessageTimeToLive,
            DuplicateDetectionHistoryTimeWindow = request.Properties.DuplicateDetectionHistoryTimeWindow,
            EnableBatchedOperations = request.Properties.EnableBatchedOperations,
            EnableExpress = request.Properties.EnableExpress,
            EnablePartitioning = request.Properties.EnablePartitioning,
            MaxMessageSizeInKilobytes = request.Properties.MaxMessageSizeInKilobytes,
            MaxSizeInMegabytes = request.Properties.MaxSizeInMegabytes,
            RequiresDuplicateDetection = request.Properties.RequiresDuplicateDetection,
            SupportOrdering = request.Properties.SupportOrdering,
        };
    }
}
