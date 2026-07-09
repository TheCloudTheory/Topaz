using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics.Endpoints;

/// <summary>
/// The azurerm Terraform provider checks for soft-deleted workspaces before creating one.
/// Topaz does not implement soft-delete, so return an empty list.
/// </summary>
internal sealed class ListDeletedWorkspacesEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.OperationalInsights";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.OperationalInsights/deletedWorkspaces"
    ];

    public string[] Permissions => ["Microsoft.OperationalInsights/workspaces/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.CreateJsonContentResponse(new DeletedWorkspacesListResponse());
    }
}

internal sealed class DeletedWorkspacesListResponse
{
    public object[] Value { get; set; } = [];

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
