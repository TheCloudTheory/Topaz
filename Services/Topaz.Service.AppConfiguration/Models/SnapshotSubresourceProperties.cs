using System.Text;
using System.Text.Json.Serialization;
using Azure;
using JetBrains.Annotations;
using Topaz.Service.AppConfiguration.Models.DataPlane;
using Topaz.Service.AppConfiguration.Models.Requests;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class SnapshotSubresourceProperties
{
    public string? CompositionType { get; set; }
    public string? Created { get; set; }
    public string? Etag { get; set; }
    public string? Expires { get; set; }
    public KeyValueFilter[]? Filters { get; set; }
    public long? ItemsCount { get; set; }
    public string? ProvisioningState { get; set; }
    public long? RetentionPeriod { get; set; }
    public long? Size { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SnapshotStatus Status { get; set; }
    public IDictionary<string, string>? Tags { get; set; }

    [UsedImplicitly]
    internal sealed class KeyValueFilter
    {
        public string? Key { get; set; }
        public string? Label { get; set; }
    }
    
    private const long DefaultRetentionPeriod = 2_592_000;

    public static SnapshotSubresourceProperties From(CreateSnapshotRequest request, List<AppConfigurationKeyValue> kvs)
    {
        return new SnapshotSubresourceProperties
        {
            CompositionType = request.Properties?.CompositionType,
            Created = DateTimeOffset.Now.ToString(),
            Etag = new ETag(DateTimeOffset.Now.Ticks.ToString()).ToString(),
            Expires = null,
            Filters = request.Properties?.Filters,
            ItemsCount = kvs.Count,
            ProvisioningState = "Succeeded",
            RetentionPeriod = request.Properties?.RetentionPeriod ?? DefaultRetentionPeriod,
            Size = kvs.Sum(kv =>
                Encoding.UTF8.GetByteCount(kv.Key) +
                (kv.Value is null ? 0 : Encoding.UTF8.GetByteCount(kv.Value))),
            Status = SnapshotStatus.Ready,
            Tags = request.Properties?.Tags
        };
    }

    internal enum SnapshotStatus
    {
        Provisioning,
        Ready,
        Archived,
        Failed
    }
}