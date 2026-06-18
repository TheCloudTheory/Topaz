using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Documents;

internal sealed class PatchDocumentEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public PatchDocumentEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private PatchDocumentEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["PATCH /dbs/{db}/colls/{coll}/docs/{docId}"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];
        var docId = Uri.UnescapeDataString(segments[5]);

        PatchDocumentRequest? patchRequest;
        try
        {
            patchRequest = JsonSerializer.Deserialize<PatchDocumentRequest>(
                context.Request.Body, GlobalSettings.JsonOptions);
        }
        catch (Exception)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        if (patchRequest == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var ifMatch = context.Request.Headers["If-Match"].ToString();
        var result = _dataPlane.PatchDocument(context, databaseName, collectionName, docId, patchRequest,
            string.IsNullOrEmpty(ifMatch) ? null : ifMatch);

        switch (result.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.PreconditionFailed:
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                return;
        }

        response.Headers.Add("x-ms-request-charge", "1");
        response.CreateJsonContentResponse(result.Resource!);
    }
}
