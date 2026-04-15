using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed class LeaseContainerEndpoint(ITopazLogger logger)
    : BlobDataPlaneEndpointBase(logger), IEndpointDefinition
{
    private readonly BlobServiceDataPlane _dataPlane =
        new(new BlobServiceControlPlane(new BlobResourceProvider(logger)), logger);

    public string[] Endpoints => ["PUT /{containerName}?restype=container&comp=lease"];

    public string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/write"];

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
            var containerName = GetContainerName(context.Request.Path);
            var headers = context.Request.Headers;

            var leaseAction = headers.TryGetValue("x-ms-lease-action", out var actionValues)
                ? actionValues.ToString()
                : string.Empty;

            if (string.IsNullOrEmpty(leaseAction))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent("x-ms-lease-action header is required.");
                return;
            }

            var leaseDuration = headers.TryGetValue("x-ms-lease-duration", out var durationValues) &&
                                int.TryParse(durationValues.ToString(), out var parsedDuration)
                ? parsedDuration
                : 0;

            var proposedLeaseId = headers.TryGetValue("x-ms-proposed-lease-id", out var proposedValues)
                ? proposedValues.ToString()
                : null;

            var currentLeaseId = headers.TryGetValue("x-ms-lease-id", out var leaseIdValues)
                ? leaseIdValues.ToString()
                : null;

            int? breakPeriod = headers.TryGetValue("x-ms-lease-break-period", out var breakValues) &&
                               int.TryParse(breakValues.ToString(), out var parsedBreak)
                ? parsedBreak
                : null;

            Logger.LogDebug(nameof(LeaseContainerEndpoint), nameof(GetResponse),
                "Lease action '{0}' on container: {1}", leaseAction, containerName);

            var (statusCode, lease) = _dataPlane.LeaseContainer(
                subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, containerName,
                leaseAction, leaseDuration, proposedLeaseId, currentLeaseId, breakPeriod);

            response.StatusCode = statusCode;

            if (lease == null)
            {
                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
                return;
            }

            var now = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            response.Content.Headers.TryAddWithoutValidation("Last-Modified", now.ToString("R"));

            if (lease.LeaseId != null)
                response.Headers.TryAddWithoutValidation("x-ms-lease-id", lease.LeaseId);

            if (statusCode == HttpStatusCode.Accepted && lease.BreakTime.HasValue)
            {
                var remainingSeconds = (int)Math.Max(0, Math.Ceiling((lease.BreakTime.Value - now).TotalSeconds));
                response.Headers.TryAddWithoutValidation("x-ms-lease-time", remainingSeconds.ToString());
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
