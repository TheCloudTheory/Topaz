using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Disk.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Disk.Endpoints;

internal sealed class GetDiskAccessOperationStatusEndpoint : IEndpointDefinition
{
    private static readonly TimeSpan InProgressThreshold = TimeSpan.FromMilliseconds(500);

    public string ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Compute/locations/{location}/diskOperations/{operationId}"
    ];

    public string[] Permissions => ["Microsoft.Compute/locations/diskOperations/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var operationIdStr = context.Request.Path.Value.ExtractValueFromPath(8);

        if (!Guid.TryParse(operationIdStr, out var operationId))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var entry = DiskAccessLroStore.Instance.TryGet(operationId);

        if (entry == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        if (DateTimeOffset.UtcNow - entry.CreatedAt < InProgressThreshold)
        {
            response.StatusCode = HttpStatusCode.OK;
            response.CreateJsonContentResponse(new DiskAccessOperationStatusResponse { Status = "InProgress" });
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.CreateJsonContentResponse(new DiskAccessOperationStatusResponse
        {
            Status = "Succeeded",
            AccessSAS = entry.AccessSAS,
            Properties = new DiskAccessOperationStatusProperties
            {
                Output = new DiskAccessOperationOutput { AccessSAS = entry.AccessSAS }
            }
        });
    }
}
