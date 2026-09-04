namespace Topaz.Service.EventGrid.Models;

internal sealed class EventGridEventSchema
{
    public string? Id { get; init; }
    public string? Subject { get; init; }
    public string? Topic { get; init; }
    public string? EventType { get; init; }
    public string? EventTime { get; init; }
    public object? Data { get; init; }
    public string? DataVersion { get; init; }
    public string? MetadataVersion { get; init; }
}