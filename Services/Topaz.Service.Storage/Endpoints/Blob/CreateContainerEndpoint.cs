using Topaz.EventPipeline;
using System.Net;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Utils;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Blob;

internal sealed partial class CreateContainerEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : BlobDataPlaneEndpointBase(eventPipeline, logger)
{
    private readonly BlobServiceControlPlane _controlPlane = new(new BlobResourceProvider(logger));

    public string ProviderNamespace => "Microsoft.Storage";

    public override string[] Endpoints => ["PUT /{containerName}"];

    public override string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/write"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (RejectIfSecondaryHostForMutation(context.Request.Headers, response)) return;
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

        try
        {
            var containerName = GetContainerName(context.Request.Path);
            var op = _controlPlane.CreateContainer(subscriptionIdentifier, resourceGroupIdentifier, containerName,
                storageAccount!.Name, context.Request.Headers);

            response.StatusCode = op.Result switch
            {
                OperationResult.Created => HttpStatusCode.Created,
                OperationResult.Conflict => SetConflict(response),
                _ => HttpStatusCode.BadRequest
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(CreateContainerEndpoint), nameof(GetResponse), "{0}", ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }

    private static HttpStatusCode SetConflict(HttpResponseMessage response)
    {
        response.CreateBlobErrorResponse(BlobErrorCode.ContainerAlreadyExists,
            "The specified container already exists.", HttpStatusCode.Conflict);
        return HttpStatusCode.Conflict;
    }
    
    public override (bool isValid, string[]? validationErrors) Validate(HttpContext context)
    {
        var container = context.Request.Path.Value.ExtractValueFromPath(1);
        if(string.IsNullOrWhiteSpace(container))
        {
            return (false, ["filesystem is required"]);
        }
        
        if(container.Length is < 3 or > 63)
        {
            return (false, ["filesystem must be between 3 and 63 characters"]);
        }
        
        if(!ContainerRegex().IsMatch(container))
        {
            return (false, ["filesystem must start with a letter or number, and can contain letters, numbers, and dashes. It must not end with a dash, and must not contain consecutive dashes"]);
        }

        return (true, null);
    }

    [GeneratedRegex(@"^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")]
    private static partial Regex ContainerRegex();
}
