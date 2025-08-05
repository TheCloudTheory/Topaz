using Topaz.Service.EventHub.Models.Requests;

namespace Topaz.Service.EventHub.Models;

internal sealed class EventHubNamespaceResourceProperties
{
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    
    public static EventHubNamespaceResourceProperties From(CreateOrUpdateEventHubNamespaceRequest request)
    {
        return new EventHubNamespaceResourceProperties
        {
            
        };
    }
}