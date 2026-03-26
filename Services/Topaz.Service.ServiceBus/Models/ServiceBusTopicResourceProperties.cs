using System.Xml;
using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

public sealed class ServiceBusTopicResourceProperties : ServiceBusEntityResourceProperties
{
    public int? SubscriptionCount { get; set; } = 0;
    public bool? SupportOrdering { get; set; }

    public static ServiceBusTopicResourceProperties From(CreateOrUpdateServiceBusTopicRequest request)
    {
        var properties = request.Properties;
        return new ServiceBusTopicResourceProperties
        {
            CountDetails = properties?.CountDetails,
            AutoDeleteOnIdle = DeterminePropertyValue(properties?.AutoDeleteOnIdle, TimeSpan.MaxValue),
            DefaultMessageTimeToLive = DeterminePropertyValue(properties?.DefaultMessageTimeToLive, TimeSpan.MaxValue),
            DuplicateDetectionHistoryTimeWindow = DeterminePropertyValue(properties?.DuplicateDetectionHistoryTimeWindow, TimeSpan.FromMinutes(10)),
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
            properties.AutoDeleteOnIdle = requestProps.AutoDeleteOnIdle.ToString();
        
        if (requestProps.DefaultMessageTimeToLive.HasValue)
            properties.DefaultMessageTimeToLive = requestProps.DefaultMessageTimeToLive.ToString();
        
        if (requestProps.DuplicateDetectionHistoryTimeWindow.HasValue)
            properties.DuplicateDetectionHistoryTimeWindow = requestProps.DuplicateDetectionHistoryTimeWindow.ToString();
        
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
        
        properties.UpdatedOn = DateTimeOffset.UtcNow;
    }
}
