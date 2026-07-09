using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.LogAnalytics.Models;

public sealed class WorkspaceSku
{
    public string Name { get; set; } = "PerGB2018";
}

public sealed class WorkspaceFeatures
{
    public bool EnableLogAccessUsingOnlyResourcePermissions { get; set; } = true;
}

public sealed class WorkspaceResourceProperties
{
    public WorkspaceSku? Sku { get; set; }

    public int RetentionInDays { get; set; }

    public string? WorkspaceId { get; set; }

    public string? CustomerId { get; set; }

    public string ProvisioningState => "Succeeded";

    public string PublicNetworkAccessForIngestion { get; set; } = "Enabled";

    public string PublicNetworkAccessForQuery { get; set; } = "Enabled";

    public WorkspaceFeatures Features { get; set; } = new();

    public static WorkspaceResourceProperties FromRequest(WorkspaceResourceProperties? source)
    {
        var workspaceId = Guid.NewGuid().ToString();
        return new WorkspaceResourceProperties
        {
            Sku = source?.Sku ?? new WorkspaceSku { Name = "PerGB2018" },
            RetentionInDays = source?.RetentionInDays > 0 ? source.RetentionInDays : 30,
            WorkspaceId = workspaceId,
            CustomerId = workspaceId,
            PublicNetworkAccessForIngestion = source?.PublicNetworkAccessForIngestion ?? "Enabled",
            PublicNetworkAccessForQuery = source?.PublicNetworkAccessForQuery ?? "Enabled",
            Features = source?.Features ?? new WorkspaceFeatures(),
        };
    }

    internal void UpdateFromRequest(UpdateWorkspaceRequest request)
    {
        if (request.Properties?.RetentionInDays.HasValue == true)
            RetentionInDays = request.Properties.RetentionInDays.Value;
        if (request.Properties?.Sku != null)
            Sku = request.Properties.Sku;
        if (request.Properties?.PublicNetworkAccessForIngestion != null)
            PublicNetworkAccessForIngestion = request.Properties.PublicNetworkAccessForIngestion;
        if (request.Properties?.PublicNetworkAccessForQuery != null)
            PublicNetworkAccessForQuery = request.Properties.PublicNetworkAccessForQuery;
    }
}
