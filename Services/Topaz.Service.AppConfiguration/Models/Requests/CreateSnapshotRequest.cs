using JetBrains.Annotations;
using Topaz.Service.Shared;

namespace Topaz.Service.AppConfiguration.Models.Requests;

internal sealed class CreateSnapshotRequest : IValidatable
{
    public CreateSnapshotRequestProperties? Properties { get; init; }

    [UsedImplicitly]
    internal class CreateSnapshotRequestProperties
    {
        public SnapshotSubresourceProperties.KeyValueFilter[]? Filters  { get; init; }
        public string? CompositionType { get; init; }
        public long? RetentionPeriod { get; init; }
        public IDictionary<string, string>? Tags { get; init; }
        public string? Description { get; init; }
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

    public static CreateSnapshotRequest From(CreateSnapshotRequestProperties request)
    {
        return new CreateSnapshotRequest
        {
            Properties = request
        };
    }
}