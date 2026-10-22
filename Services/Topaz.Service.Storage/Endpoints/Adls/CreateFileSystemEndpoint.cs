using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Adls;

internal sealed class CreateFileSystemEndpoint(Pipeline eventPipeline, ITopazLogger logger) : AdlsDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["PUT /{filesystem}?resource=filesystem"];
    public override string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/write"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount, out _))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, Permissions, context, response))
        {
            return;
        }
    }
}