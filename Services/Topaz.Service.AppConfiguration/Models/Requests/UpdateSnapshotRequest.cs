using System.Text.Json.Serialization;

namespace Topaz.Service.AppConfiguration.Models.Requests;

internal sealed class UpdateSnapshotRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SnapshotSubresourceProperties.SnapshotStatus Status { get; set; }
}