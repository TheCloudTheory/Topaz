using Topaz.Service.LogAnalytics.Models;

namespace Topaz.Service.LogAnalytics.Models;

internal sealed class UpdateWorkspaceRequest
{
    public IDictionary<string, string>? Tags { get; set; }
    public UpdateWorkspaceRequestProperties? Properties { get; set; }
}

internal sealed class UpdateWorkspaceRequestProperties
{
    public int? RetentionInDays { get; set; }
    public WorkspaceSku? Sku { get; set; }
    public string? PublicNetworkAccessForIngestion { get; set; }
    public string? PublicNetworkAccessForQuery { get; set; }
}
