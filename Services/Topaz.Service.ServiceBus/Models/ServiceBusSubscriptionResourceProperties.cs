using System.Xml;
using JetBrains.Annotations;
using Topaz.Service.ServiceBus.Models.Requests;

namespace Topaz.Service.ServiceBus.Models;

public sealed class ServiceBusSubscriptionResourceProperties
{
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? AccessedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool? RequiresSession { get; set; } = false;
    public bool? DeadLetteringOnMessageExpiration { get; set; } = false;
    public bool? DeadLetteringOnFilterEvaluationExceptions { get; set; } = true;
    public bool? EnableBatchedOperations { get; set; } = false;
    public int? MaxDeliveryCount { get; set; } = 10;
    public string? Status { get; set; }

    public static ServiceBusSubscriptionResourceProperties From(CreateOrUpdateServiceBusSubscriptionRequest request)
    {
        var properties = request.Properties;
        return new ServiceBusSubscriptionResourceProperties
        {
            RequiresSession = properties?.RequireSession ?? false,
            DeadLetteringOnMessageExpiration  = properties?.DeadLetteringOnMessageExpiration ?? false,
            DeadLetteringOnFilterEvaluationExceptions  = properties?.DeadLetteringOnFilterEvaluationExceptions ?? true,
            EnableBatchedOperations = properties?.EnableBatchedOperations  ?? false,
            MaxDeliveryCount = properties?.MaxDeliveryCount ?? 10,
            Status = properties?.Status
        };
    }
    
    internal static void UpdateFromRequest(ServiceBusSubscriptionResource resource, CreateOrUpdateServiceBusSubscriptionRequest request)
    {
        if (request.Properties == null)
        {
            throw new ArgumentNullException(nameof(request.Properties));
        }

        var properties = resource.Properties;
        var requestProps = request.Properties;
        
        if (requestProps.EnableBatchedOperations.HasValue)
            properties.EnableBatchedOperations = requestProps.EnableBatchedOperations;
        
        if (requestProps.Status != null)
            properties.Status = requestProps.Status;
        
        properties.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
