using System.Text.Json.Serialization;

namespace Topaz.Service.ResourceManager.Models;

public sealed class OperationRecord
{
    public string Id { get; init; } = string.Empty;
    public string OperationId { get; init; } = string.Empty;
    public OperationRecordProperties Properties { get; init; } = new();

    public override string ToString() =>
        System.Text.Json.JsonSerializer.Serialize(this, Topaz.Shared.GlobalSettings.JsonOptions);

    public static OperationRecord Create(
        string deploymentId,
        string resourceId,
        string resourceType,
        string resourceName,
        bool succeeded,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        var operationId = Guid.NewGuid().ToString("N").ToUpperInvariant();
        return new OperationRecord
        {
            Id = $"{deploymentId}/operations/{operationId}",
            OperationId = operationId,
            Properties = new OperationRecordProperties
            {
                ProvisioningState = succeeded ? "Succeeded" : "Failed",
                Timestamp = start,
                Duration = end - start,
                StatusCode = succeeded ? "OK" : "Conflict",
                TargetResource = new OperationTargetResource
                {
                    Id = resourceId,
                    ResourceType = resourceType,
                    ResourceName = resourceName
                }
            }
        };
    }
}

public sealed class OperationRecordProperties
{
    public string ProvisioningOperation { get; init; } = "Create";
    public string ProvisioningState { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }

    [JsonConverter(typeof(IsoDurationConverter))]
    public TimeSpan Duration { get; init; }

    public string StatusCode { get; init; } = "OK";
    public OperationTargetResource? TargetResource { get; init; }
}

public sealed class OperationTargetResource
{
    public string Id { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public string ResourceName { get; init; } = string.Empty;
}

/// <summary>Serializes TimeSpan as ISO 8601 duration (e.g. "PT5.123S").</summary>
internal sealed class IsoDurationConverter : System.Text.Json.Serialization.JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert,
        System.Text.Json.JsonSerializerOptions options)
        => System.Xml.XmlConvert.ToTimeSpan(reader.GetString()!);

    public override void Write(System.Text.Json.Utf8JsonWriter writer, TimeSpan value,
        System.Text.Json.JsonSerializerOptions options)
        => writer.WriteStringValue(System.Xml.XmlConvert.ToString(value));
}
