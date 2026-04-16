using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models;
using Topaz.Service.Storage.Utils;
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

            var op = _dataPlane.LeaseContainer(
                subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount!.Name, containerName,
                leaseAction, leaseDuration, proposedLeaseId, currentLeaseId, breakPeriod);

            response.StatusCode = op.Result switch
            {
                OperationResult.Created => HttpStatusCode.Created,
                OperationResult.Accepted => HttpStatusCode.Accepted,
                OperationResult.Success => HttpStatusCode.OK,
                OperationResult.NotFound => HttpStatusCode.NotFound,
                OperationResult.Conflict => HttpStatusCode.Conflict,
                OperationResult.PreconditionFailed => HttpStatusCode.PreconditionFailed,
                OperationResult.BadRequest => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            if (op.Resource == null)
            {
                response.CreateBlobErrorResponse(
                    GetLeaseErrorCode(op.Result, leaseAction, currentLeaseId),
                    GetLeaseErrorMessage(op.Result, leaseAction, containerName, currentLeaseId),
                    response.StatusCode);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            response.Content.Headers.TryAddWithoutValidation("Last-Modified", now.ToString("R"));

            if (op.Resource.LeaseId != null)
                response.Headers.TryAddWithoutValidation("x-ms-lease-id", op.Resource.LeaseId);

            if (op.Result == OperationResult.Accepted && op.Resource.BreakTime.HasValue)
            {
                var remainingSeconds = (int)Math.Max(0, Math.Ceiling((op.Resource.BreakTime.Value - now).TotalSeconds));
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

    private static string GetLeaseErrorCode(OperationResult result, string leaseAction, string? currentLeaseId)
    {
        return result switch
        {
            OperationResult.NotFound => "ContainerNotFound",
            OperationResult.PreconditionFailed => "LeaseIdMismatchWithContainerOperation",
            OperationResult.BadRequest when string.Equals(leaseAction, "change", StringComparison.OrdinalIgnoreCase) =>
                "InvalidHeaderValue",
            OperationResult.Conflict when string.Equals(leaseAction, "acquire", StringComparison.OrdinalIgnoreCase) =>
                "LeaseAlreadyPresent",
            OperationResult.Conflict when RequiresLeaseId(leaseAction) && string.IsNullOrWhiteSpace(currentLeaseId) =>
                "LeaseIdMissing",
            OperationResult.Conflict => "LeaseNotPresentWithContainerOperation",
            _ => "OperationNotAllowed"
        };
    }

    private static string GetLeaseErrorMessage(OperationResult result, string leaseAction, string containerName,
        string? currentLeaseId)
    {
        return result switch
        {
            OperationResult.NotFound => $"Container '{containerName}' was not found.",
            OperationResult.PreconditionFailed => "The specified lease ID did not match the active lease.",
            OperationResult.BadRequest when string.Equals(leaseAction, "change", StringComparison.OrdinalIgnoreCase) =>
                "A proposed lease ID is required when changing a lease.",
            OperationResult.Conflict when string.Equals(leaseAction, "acquire", StringComparison.OrdinalIgnoreCase) =>
                "There is already an active lease on the container.",
            OperationResult.Conflict when RequiresLeaseId(leaseAction) && string.IsNullOrWhiteSpace(currentLeaseId) =>
                "A lease ID must be specified for this lease operation.",
            OperationResult.Conflict => "There is no active lease on the container.",
            _ => $"Lease action '{leaseAction}' is not allowed."
        };
    }

    private static bool RequiresLeaseId(string leaseAction) =>
        string.Equals(leaseAction, "renew", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(leaseAction, "change", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(leaseAction, "release", StringComparison.OrdinalIgnoreCase);
}
