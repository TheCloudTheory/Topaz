namespace Topaz.Host.AMQP;

/// <summary>
/// Various keys used by the processor when responding with service configuration.
/// </summary>
public static class ResponseMap
{
    public static string Name { get; } = "name";
    public static string CreatedAt { get; } = "created_at";
    public static string PartitionIdentifier { get; } = "partition";
    public static string PartitionIdentifiers { get; } = "partition_ids";
    public static string GeoReplicationFactor { get; } = "georeplication_factor";
    public static string PartitionBeginSequenceNumber { get; } = "begin_sequence_number";
    public static string PartitionLastEnqueuedSequenceNumber { get; } = "last_enqueued_sequence_number";
    public static string PartitionLastEnqueuedOffset { get; } = "last_enqueued_offset";
    public static string PartitionLastEnqueuedTimeUtc { get; } = "last_enqueued_time_utc";
    public static string PartitionRuntimeInfoRetrievalTimeUtc { get; } = "runtime_info_retrieval_time_utc";
    public static string PartitionRuntimeInfoPartitionIsEmpty { get; } = "is_partition_empty";
}