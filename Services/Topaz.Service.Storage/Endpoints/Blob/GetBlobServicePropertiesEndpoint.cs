using Topaz.EventPipeline;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class GetBlobServicePropertiesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : BlobDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["GET /?restype=service&comp=properties"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/read"];

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount, out _))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, Permissions, context, response))
            return;

        var result = _controlPlane.GetBlobServicePropertiesXml(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccount.Name);

        response.Content = new StringContent(result.Resource!, Encoding.UTF8, "application/xml");
        response.StatusCode = HttpStatusCode.OK;
    }
}
