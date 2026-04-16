using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Serialization;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class GetBlockListEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["GET /{containerName}/...?comp=blocklist"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/blobs/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultBlobStoragePort], Protocol.Http);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        try
        {
            // Default is "committed" per Azure REST API spec
            context.Request.QueryString.TryGetValueForKey("blocklisttype", out var blockListType);
            blockListType = string.IsNullOrEmpty(blockListType) ? "committed" : blockListType.ToLowerInvariant();

            Logger.LogDebug(nameof(GetBlockListEndpoint), nameof(GetResponse),
                "Getting block list for {0}, type={1}.", context.Request.Path.Value, blockListType);

            var op = _dataPlane.GetBlockList(
                subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, context.Request.Path.Value!, blockListType);

            if (op.Result == OperationResult.NotFound)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found", HttpStatusCode.NotFound);
                return;
            }

            var result = BlockListResult.From(op.Resource!.Committed, op.Resource!.Uncommitted);
            using var sw = new EncodingAwareStringWriter();
            new XmlSerializer(typeof(BlockListResult)).Serialize(sw, result);
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(sw.ToString(), Encoding.UTF8);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
