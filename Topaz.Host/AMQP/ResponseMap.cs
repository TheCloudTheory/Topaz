namespace Topaz.Host.AMQP;

/// <summary>
/// AMQP response field names for Event Hub and Service Bus management operations.
/// NOTE: Different Azure SDKs have different expectations for field names:
/// - .NET SDK (azure-messaging-eventhubs) expects snake_case
/// - Node.js SDK (@azure/event-hubs) expects camelCase
/// 
/// SOLUTION: Return BOTH field name variants in the response Map.
/// The AMQP message will include redundant fields with different naming conventions.
/// Each SDK will use the field names it expects and ignore the others.
/// </summary>
public static class ResponseMap
{
    // === .NET SDK Expects Snake_Case ===
    public static string Name { get; } = "name";
    public static string CreatedAt { get; } = "created_at";
    public static string PartitionIdentifier { get; } = "partition";
    public static string PartitionIdentifiersSnakeCase { get; } = "partition_ids";
    public static string GeoReplicationFactor { get; } = "georeplication_factor";
    public static string PartitionBeginSequenceNumber { get; } = "begin_sequence_number";
    public static string PartitionLastEnqueuedSequenceNumber { get; } = "last_enqueued_sequence_number";
    public static string PartitionLastEnqueuedOffset { get; } = "last_enqueued_offset";
    public static string PartitionLastEnqueuedTimeUtc { get; } = "last_enqueued_time_utc";
    public static string PartitionRuntimeInfoRetrievalTimeUtc { get; } = "runtime_info_retrieval_time_utc";
    public static string PartitionRuntimeInfoPartitionIsEmpty { get; } = "is_partition_empty";
    
    // === Node.js SDK Expects CamelCase ===
    public static string EventHubName { get; } = "eventHubName";
    public static string CreatedOnCamelCase { get; } = "createdOn";
    public static string PartitionIdCamelCase { get; } = "partitionId";
    public static string PartitionIdentifiersCamelCase { get; } = "partitionIds";
    public static string BeginningSequenceNumber { get; } = "beginningSequenceNumber";
    public static string LastEnqueuedSequenceNumberCamelCase { get; } = "lastEnqueuedSequenceNumber";
    public static string LastEnqueuedOffsetCamelCase { get; } = "lastEnqueuedOffset";
    public static string LastEnqueuedOnUtc { get; } = "lastEnqueuedOnUtc";
    public static string RetrievalTimeUtc { get; } = "retrievalTimeUtc";
    public static string IsEmpty { get; } = "isEmpty";
}