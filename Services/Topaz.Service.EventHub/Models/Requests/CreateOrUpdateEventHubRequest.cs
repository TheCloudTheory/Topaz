namespace Topaz.Service.EventHub.Models.Requests;

public class CreateOrUpdateEventHubRequest
{
	public EventHubRequestProperties? Properties { get; init; }

	public sealed class EventHubRequestProperties
	{
		public int? MessageRetentionInDays { get; init; }
		public int? PartitionCount { get; init; }
		public string? Status { get; init; }
	}
}