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

internal sealed class GetPageRangesEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["GET /{containerName}/...?comp=pagelist"];

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
            var rangeHeader = context.Request.Headers.TryGetValue("x-ms-range", out var xMsRange)
                ? xMsRange.ToString()
                : context.Request.Headers.TryGetValue("Range", out var range)
                    ? range.ToString()
                    : null;

            long? startByte = null;
            long? endByte = null;

            if (!string.IsNullOrWhiteSpace(rangeHeader) &&
                !TryParseRange(rangeHeader, out startByte, out endByte))
            {
                response.CreateBlobErrorResponse(BlobErrorCode.InvalidRange, "Invalid range header.", HttpStatusCode.BadRequest);
                return;
            }

            var op = _dataPlane.GetPageRanges(
                subscriptionIdentifier,
                resourceGroupIdentifier,
                storageAccount.Name,
                context.Request.Path.Value!,
                startByte,
                endByte);

            if (op.Result == OperationResult.NotFound)
            {
                response.CreateBlobErrorResponse(BlobErrorCode.BlobNotFound, "Blob not found.", HttpStatusCode.NotFound);
                return;
            }

            if (op.Result == OperationResult.BadRequest)
            {
                response.CreateBlobErrorResponse(op.Code ?? BlobErrorCode.InvalidBlobType.ToString(), op.Reason ?? "Bad request.", HttpStatusCode.BadRequest);
                return;
            }

            using var sw = new EncodingAwareStringWriter();
            new XmlSerializer(typeof(PageListResult)).Serialize(sw, PageListResult.From(op.Resource!.PageRanges));

            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(sw.ToString(), Encoding.UTF8);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");

            var blobProperties = op.Resource.BlobProperties;
            response.Headers.TryAddWithoutValidation("x-ms-blob-content-length", blobProperties.ContentLength.ToString());

            var etag = blobProperties.ETag.ToString();
            response.Headers.ETag = new EntityTagHeaderValue(etag.StartsWith('"') ? etag : $"\"{etag}\"");

            if (!string.IsNullOrEmpty(blobProperties.LastModified))
            {
                response.Content.Headers.TryAddWithoutValidation("Last-Modified", blobProperties.LastModified);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }

    private static bool TryParseRange(string rangeHeader, out long? startByte, out long? endByte)
    {
        startByte = null;
        endByte = null;

        var withoutPrefix = rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase)
            ? rangeHeader["bytes=".Length..]
            : rangeHeader;

        var parts = withoutPrefix.Split('-');
        if (parts.Length != 2 ||
            !long.TryParse(parts[0], out var parsedStart) ||
            !long.TryParse(parts[1], out var parsedEnd))
        {
            return false;
        }

        startByte = parsedStart;
        endByte = parsedEnd;
        return true;
    }
}
