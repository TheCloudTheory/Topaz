using Topaz.Service.EventHub.Models.Requests;

namespace Topaz.Service.EventHub.Models;

internal sealed class EventHubResourceProperties
{
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    
    public static EventHubResourceProperties From(CreateOrUpdateEventHubRequest request)
    {
        return new EventHubResourceProperties();
    }
}