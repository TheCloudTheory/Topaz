using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Documents;

internal sealed class DeleteDocumentEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public DeleteDocumentEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private DeleteDocumentEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["DELETE /dbs/{db}/colls/{coll}/docs/{docId}"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];
        var docId = Uri.UnescapeDataString(segments[5]);

        var partitionKeyHeader = context.Request.Headers["x-ms-documentdb-partitionkey"].ToString();
        var ifMatch = context.Request.Headers["If-Match"].ToString();

        var result = _dataPlane.DeleteDocument(context, databaseName, collectionName, docId,
            partitionKeyHeader, string.IsNullOrEmpty(ifMatch) ? null : ifMatch);

        switch (result.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.BadRequest:
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            case OperationResult.PreconditionFailed:
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
        response.Headers.Add("x-ms-request-charge", "1");
        response.Headers.Add("x-ms-session-token", "0:-1#1");
    }
}
