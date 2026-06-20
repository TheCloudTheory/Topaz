using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Documents;

internal sealed class GetDocumentEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public GetDocumentEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private GetDocumentEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["GET /dbs/{db}/colls/{coll}/docs/{docId}"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        
        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];
        var docId = Uri.UnescapeDataString(segments[5]);

        var partitionKeyHeader = context.Request.Headers["x-ms-documentdb-partitionkey"].ToString();

        var result = _dataPlane.GetDocument(context, databaseName, collectionName, docId, partitionKeyHeader);

        if (result.Result == OperationResult.NotFound)
        {
            var notFoundBody = new System.Text.Json.Nodes.JsonObject
            {
                ["code"] = "NotFound",
                ["message"] = result.Reason ?? "Entity with the specified id does not exist in the system."
            };
            response.CreateJsonContentResponse(notFoundBody, HttpStatusCode.NotFound);
            return;
        }

        if (result.Result == OperationResult.BadRequest)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        response.Headers.Add("x-ms-request-charge", "1");
        response.Headers.Add("x-ms-session-token", "0:1");
        response.CreateJsonContentResponse(result.Resource!);
    }
}
