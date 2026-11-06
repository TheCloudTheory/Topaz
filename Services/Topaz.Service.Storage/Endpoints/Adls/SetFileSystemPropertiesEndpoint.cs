using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Adls;

internal sealed partial class SetFileSystemPropertiesEndpoint(Pipeline eventPipeline, ITopazLogger logger) : AdlsDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["PATCH /{filesystem}?resource=filesystem"];
    
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

        if (!IsRequestAuthorized(subscriptionIdentifier, resourceGroupIdentifier, storageAccount.Name, Permissions, context, response))
        {
            return;
        }
        
        var filesystem = context.Request.Path.Value.ExtractValueFromPath(1)!;
        var apiVersion = context.Request.Query["api-version"].ToString();
        var op = ControlPlane.SetFileSystemProperties(storageAccount, filesystem, apiVersion, context.Request.Headers);

        switch (op.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.Conflict:
                response.CreateNotFoundResponse(op);
                return;
            default:
                response.StatusCode = HttpStatusCode.OK;
                response.AppendHeaders(op);
                break;
        }
    }
    
    public override (bool isValid, string[]? validationErrors) Validate(HttpContext context)
    {
        var filesystem = context.Request.Path.Value.ExtractValueFromPath(1);
        if(string.IsNullOrWhiteSpace(filesystem))
        {
            return (false, ["filesystem is required"]);
        }
        
        if(filesystem.Length is < 3 or > 63)
        {
            return (false, ["filesystem must be between 3 and 63 characters"]);
        }
        
        if(!FileSystemRegex().IsMatch(filesystem))
        {
            return (false, ["filesystem must start with a letter or number, and can contain letters, numbers, and dashes. It must not end with a dash, and must not contain consecutive dashes"]);
        }

        return (true, null);
    }

    [GeneratedRegex(@"^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")]
    private static partial Regex FileSystemRegex();
}