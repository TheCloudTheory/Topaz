using Topaz.EventPipeline;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class SetContainerAclEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : BlobDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string? ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["PUT /{containerName}?restype=container&comp=acl"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/write"];

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (RejectIfSecondaryHostForMutation(context.Request.Headers, response)) return;
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount!.Name, Permissions, context, response))
            return;

        try
        {
            var containerName = GetContainerName(context.Request.Path);

            Logger.LogDebug(nameof(SetContainerAclEndpoint), nameof(GetResponse),
                "Setting ACL for container: {0}", containerName);

            var op = _dataPlane.SetContainerAcl(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, containerName, context.Request.Body, context.Request.Headers);

            response.StatusCode = op.Result == OperationResult.Updated ? HttpStatusCode.OK : HttpStatusCode.NotFound;

            if (op.Result != OperationResult.Updated) return;

            var now = DateTimeOffset.UtcNow;
            response.Headers.ETag = new EntityTagHeaderValue($"\"{now.Ticks}\"");
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            response.Content.Headers.TryAddWithoutValidation("Last-Modified", now.ToString("R"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
