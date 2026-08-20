using JetBrains.Annotations;
using Topaz.Service.Shared;

namespace Topaz.Service.AppConfiguration.Models.Requests;

internal sealed class CreateSnapshotRequest : IValidatable
{
    public CreateSnapshotRequestProperties? Properties { get; set; }

    [UsedImplicitly]
    internal class CreateSnapshotRequestProperties
    {
        public SnapshotSubresourceProperties.KeyValueFilter[]? Filters  { get; set; }
        public string? CompositionType { get; set; }
        public long? RetentionPeriod { get; set; }
        public IDictionary<string, string>? Tags { get; set; }
        public string? Description { get; set; }
    }

    public (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class
    {
        if (Properties == null)
        {
            return (false, null);
        }

        if (Properties?.Filters == null || Properties.Filters.Length == 0)
        {
            return (false, "You must provide at least one filter");
        }

        return Properties.RetentionPeriod is < 3600 or > 7776000
            ? (false, "Retention period must be between 3600 and 7776000 seconds.")
            : new ValueTuple<bool, string?>(true, null);
    }
}