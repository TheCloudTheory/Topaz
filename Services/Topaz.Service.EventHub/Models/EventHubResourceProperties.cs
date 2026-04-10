using Topaz.Service.EventHub.Models.Requests;

namespace Topaz.Service.EventHub.Models;

internal sealed class EventHubResourceProperties
{
    public DateTimeOffset? CreatedOn { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
    public int MessageRetentionInDays { get; set; } = 1;
    public int PartitionCount { get; set; } = 1;
    public IReadOnlyList<string> PartitionIds { get; set; } = ["0"];
    public string Status { get; set; } = "Active";
    
    public static EventHubResourceProperties From(CreateOrUpdateEventHubRequest request)
    {
        var partitionCount = request.Properties?.PartitionCount ?? 1;
        if (partitionCount < 1)
        {
            partitionCount = 1;
        }

        return new EventHubResourceProperties
        {
            MessageRetentionInDays = request.Properties?.MessageRetentionInDays ?? 1,
            PartitionCount = partitionCount,
            PartitionIds = Enumerable.Range(0, partitionCount).Select(x => x.ToString()).ToArray(),
            Status = request.Properties?.Status ?? "Active"
        };
    }
}