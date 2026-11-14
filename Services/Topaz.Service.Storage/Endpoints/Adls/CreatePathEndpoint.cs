using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Storage.Endpoints.Adls;

internal sealed partial class CreatePathEndpoint(Pipeline eventPipeline, ITopazLogger logger) : AdlsDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["PUT /{filesystem}/{path}"];
    
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
        var path = context.Request.Path.Value.ExtractValueFromPath(2)!;
        var apiVersion = context.Request.Query["api-version"].ToString();
        var op = ControlPlane.CreatePath(storageAccount, filesystem, path, context.Request.Headers, apiVersion, context.Request.Body);

        if (op.Result == OperationResult.Conflict)
        {
            response.CreateErrorResponse(op, HttpStatusCode.Conflict);
            return;
        }
        
        response.StatusCode = HttpStatusCode.Created;
        response.AppendHeaders(op);
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
        
        var path = context.Request.Path.Value.ExtractValueFromPath(2);
        if (string.IsNullOrWhiteSpace(path))
        {
            return (false, ["path is required"]);
        }

        return (true, null);
    }

    [GeneratedRegex(@"^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")]
    private static partial Regex FileSystemRegex();
}