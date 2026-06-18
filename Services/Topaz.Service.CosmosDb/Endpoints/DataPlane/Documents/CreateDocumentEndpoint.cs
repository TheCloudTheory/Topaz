using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Documents;

internal sealed class CreateDocumentEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public CreateDocumentEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private CreateDocumentEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["POST /dbs/{db}/colls/{coll}/docs"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        // Delegate query requests to the (not-yet-implemented) query endpoint
        if (string.Equals(context.Request.Headers["x-ms-documentdb-isquery"].ToString(), "true",
                StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = HttpStatusCode.NotImplemented;
            return;
        }

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];

        JsonObject? body;
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var raw = reader.ReadToEnd();
            body = JsonNode.Parse(raw)?.AsObject();
        }
        catch (Exception)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        if (body == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _dataPlane.CreateDocument(context, databaseName, collectionName, body);

        switch (result.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.BadRequest:
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            case OperationResult.Conflict:
                response.StatusCode = HttpStatusCode.Conflict;
                return;
        }

        response.Headers.Add("x-ms-request-charge", "1");
        response.CreateJsonContentResponse(result.Resource!, HttpStatusCode.Created);
    }
}
