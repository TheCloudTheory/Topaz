using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Adls;

internal sealed class ListFilesystemsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : AdlsDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["GET /?resource=account"];
    
    public override string[] Permissions => ["Microsoft.Storage/storageAccounts/blobServices/containers/read"];

    public override async Task GetResponseAsync(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount, out _))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount.GetResourceGroup();

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name, Permissions, context, response))
        {
            return;
        }
        
        var apiVersion = context.Request.Query["api-version"].ToString();
        int? maxResults = int.TryParse(context.Request.Query["maxResults"].ToString(), out var parsedMaxResults)
            ? parsedMaxResults
            : null;
        var prefix = context.Request.Query["prefix"].ToString();
        var timeout = context.Request.Query["timeout"].ToString();
        var cts = new CancellationTokenSource();
        if (!string.IsNullOrEmpty(timeout))
        {
            cts.CancelAfter(TimeSpan.FromSeconds(int.Parse(timeout)));
        }
        
        var op = await ControlPlane.ListFilesystems(storageAccount, apiVersion, cts.Token);

        switch (op.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.Conflict:
                response.CreateNotFoundResponse(op);
                return;
            default:
                response.CreateJsonContentResponse(FilesystemsListResponse.From(op.Resource!, maxResults, prefix));
                response.AppendHeaders(op);
                break;
        }
    }
}