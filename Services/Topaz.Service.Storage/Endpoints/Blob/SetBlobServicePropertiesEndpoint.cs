using Topaz.EventPipeline;
using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class SetBlobServicePropertiesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : BlobDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["PUT /?restype=service&comp=properties"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/write"];

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (RejectIfSecondaryHostForMutation(context.Request.Headers, response)) return;
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount, out _))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, Permissions, context, response))
            return;

        _controlPlane.SetBlobServiceProperties(subscriptionIdentifier, resourceGroupIdentifier,
            storageAccount.Name, context.Request.Body);

        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = null;
        response.StatusCode = HttpStatusCode.Accepted;
    }
}
