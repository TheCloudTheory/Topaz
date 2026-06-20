using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Documents;

internal sealed class ReplaceDocumentEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public ReplaceDocumentEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private ReplaceDocumentEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["PUT /dbs/{db}/colls/{coll}/docs/{docId}"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];
        var docId = Uri.UnescapeDataString(segments[5]);

        JsonObject? body;
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            body = JsonNode.Parse(reader.ReadToEnd())?.AsObject();
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

        var ifMatch = context.Request.Headers["If-Match"].ToString();
        var result = _dataPlane.ReplaceDocument(context, databaseName, collectionName, docId, body,
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
        response.Headers.Add("x-ms-session-token", "0:1");
        response.CreateJsonContentResponse(result.Resource!);
    }
}
