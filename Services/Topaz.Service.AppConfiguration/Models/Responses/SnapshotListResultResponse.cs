using Topaz.Service.Shared;

namespace Topaz.Service.AppConfiguration.Models.Responses;

internal sealed class SnapshotListResultResponse : TopazApiModel
{
    public SnapshotSubresourceProperties[] Items { get; set; } = [];

    public static SnapshotListResultResponse From(SnapshotFullSubresource[] snapshots, string? selectFields)
    {
        if (!string.IsNullOrWhiteSpace(selectFields))
        {
            var selected = selectFields.Split(',');
            var items = snapshots.Select(s => new SnapshotSubresourceProperties
            {
                Name = selected.Contains("name") ? s.Properties.Name : null,
                Status = selected.Contains("status") ? s.Properties.Status : null,
                Created = selected.Contains("created") ? s.Properties.Created : null,
                CompositionType = selected.Contains("composition_type") ? s.Properties.CompositionType : null,
                Filters = selected.Contains("filters") ? s.Properties.Filters : null,
                Expires = selected.Contains("expires") ? s.Properties.Expires : null,
                RetentionPeriod = selected.Contains("retention_period") ? s.Properties.RetentionPeriod : null,
                Size = selected.Contains("size") ? s.Properties.Size : null,
                ItemsCount = selected.Contains("items_count") ? s.Properties.ItemsCount : null,
                Tags = selected.Contains("tags") ? s.Properties.Tags : null,
                Description = selected.Contains("description") ? s.Properties.Description : null,
                Etag = selected.Contains("etag") ? s.Properties.Etag : null
            });
            
            return new SnapshotListResultResponse
            {
                Items = [.. items],
            };
        }
        
        return new SnapshotListResultResponse
        {
            Items = [.. snapshots.Select(s => s.Properties)],
        };
    }
}