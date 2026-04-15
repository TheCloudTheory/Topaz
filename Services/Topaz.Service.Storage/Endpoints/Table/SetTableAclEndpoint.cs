using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class SetTableAclEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["PUT /{tableName}"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultTableStoragePort], Protocol.Http);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                context.Request.Headers, context.Request.Method, context.Request.Path, context.Request.QueryString))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }

        if (context.Request.QueryString.TryGetValueForKey("comp", out var comp) && comp == "acl")
        {
            var tableName = context.Request.Path.Value!.Replace("/", string.Empty);
            var aclOp = ControlPlane.SetAcl(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name,
                tableName, context.Request.Body);
            response.StatusCode = aclOp.Result == OperationResult.BadRequest
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.NoContent;
            return;
        }

        response.StatusCode = HttpStatusCode.NotFound;
    }
}
