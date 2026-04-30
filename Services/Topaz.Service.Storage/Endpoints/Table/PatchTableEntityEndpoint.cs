using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

internal sealed class PatchTableEntityEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => [@"PATCH /^.*?\(PartitionKey='.*?',(%20|\s)?RowKey='.*?'\)$"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/tableServices/tables/entities/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultTableStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name, context))
        {
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }

        var matches = Regex.Match(context.Request.Path, @"\w+\(PartitionKey='\w+',(%20|\s)?RowKey='\w+'\)$",
            RegexOptions.IgnoreCase);

        HandleUpdateEntityRequest(context.Request.Body, context.Request.Headers, matches,
            subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name, response);
    }
}
