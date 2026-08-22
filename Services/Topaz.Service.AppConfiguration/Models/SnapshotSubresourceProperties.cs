using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Azure;
using JetBrains.Annotations;
using Topaz.Service.AppConfiguration.Models.DataPlane;
using Topaz.Service.AppConfiguration.Models.Requests;
using Topaz.Service.Shared;

namespace Topaz.Service.AppConfiguration.Models;

internal sealed class SnapshotSubresourceProperties : TopazApiModel
{
    public string? Name { get; set; }
    [JsonPropertyName("composition_type")]
    public string? CompositionType { get; set; }
    public string? Created { get; set; }
    public string? Etag { get; set; }
    public string? Expires { get; set; }
    public KeyValueFilter[]? Filters { get; set; }
    
    [JsonPropertyName("items_count")]
    public long? ItemsCount { get; set; }
    public string? ProvisioningState { get; set; }
    
    [JsonPropertyName("retention_period")]
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

    public static SnapshotSubresourceProperties From(string name, CreateSnapshotRequest request, List<AppConfigurationKeyValue> kvs)
    {
        return new SnapshotSubresourceProperties
        {
            CompositionType = request.Properties?.CompositionType,
            Created = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            Etag = new ETag(DateTimeOffset.Now.Ticks.ToString()).ToString(),
            Expires = null,
            Filters = request.Properties?.Filters,
            ItemsCount = kvs.Count,
            Name = name,
            ProvisioningState = "Succeeded",
            RetentionPeriod = request.Properties?.RetentionPeriod ?? SnapshotSubresource.DefaultRetentionPeriod,
            Size = kvs.Sum(kv =>
                Encoding.UTF8.GetByteCount(kv.Key) +
                (kv.Value is null ? 0 : Encoding.UTF8.GetByteCount(kv.Value))),
            Status = SnapshotStatus.ready,
            Tags = request.Properties?.Tags
        };
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum SnapshotStatus
    {
        provisioning,
        ready,
        archived,
        failed
    }
}