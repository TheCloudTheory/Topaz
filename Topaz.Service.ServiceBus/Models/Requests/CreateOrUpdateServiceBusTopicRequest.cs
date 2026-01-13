using JetBrains.Annotations;
using Topaz.ResourceManager;

namespace Topaz.Service.ServiceBus.Models.Requests;

internal sealed class CreateOrUpdateServiceBusTopicRequest
{
    public CreateOrUpdateServiceBusTopicRequestProperties? Properties { get; private init; } = new();
    
    [UsedImplicitly]
    public class CreateOrUpdateServiceBusTopicRequestProperties
    {
        public object? CountDetails { get; init; }
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
        public bool? SupportOrdering { get; init; }
    }

    public static CreateOrUpdateServiceBusTopicRequest From(GenericResource resource)
    {
        return new CreateOrUpdateServiceBusTopicRequest
        {
            Properties = resource.Properties as CreateOrUpdateServiceBusTopicRequestProperties
        };
    }
}
