using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.SqlQuery;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Documents;

internal sealed class CreateDocumentEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;
    private readonly ITopazLogger _logger;

    public CreateDocumentEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), eventPipeline, logger) { }

    private CreateDocumentEndpoint(CosmosDbDataPlane dataPlane, Pipeline eventPipeline, ITopazLogger logger)
        : base(dataPlane, eventPipeline, logger)
    {
        _dataPlane = dataPlane;
        _logger = logger;
    }

    public override string[] Endpoints => ["POST /dbs/{db}/colls/{coll}/docs"];
    public override string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/write"];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];

        if (string.Equals(context.Request.Headers["x-ms-documentdb-isquery"].ToString(), "true",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.Request.ContentType, "application/query+json", StringComparison.OrdinalIgnoreCase) ||
            (context.Request.ContentType?.StartsWith("application/query+json", StringComparison.OrdinalIgnoreCase) == true))
        {
            CosmosDbSqlQueryRequest? queryRequest;
            string rawBody;
            try
            {
                using var queryReader = new StreamReader(context.Request.Body);
                rawBody = queryReader.ReadToEnd();
                queryRequest = JsonSerializer.Deserialize<CosmosDbSqlQueryRequest>(
                    rawBody, GlobalSettings.JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(nameof(CreateDocumentEndpoint), nameof(GetResponse), "Failed to parse query request body: {0}", ex.Message);
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            if (queryRequest == null || string.IsNullOrWhiteSpace(queryRequest.Query))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            // Query plan requests: the Cosmos SDK sends this before executing ORDER BY / complex
            // cross-partition queries. Return a minimal single-range plan so the SDK uses
            // parallel (non-merging) execution and sends the original query unchanged.
            if (string.Equals(context.Request.Headers["x-ms-cosmos-is-query-plan-request"].ToString(), "True",
                    StringComparison.OrdinalIgnoreCase))
            {
                response.Headers.Add("x-ms-request-charge", "1");
                response.CreateJsonContentResponse(CosmosDbDataPlane.GetQueryPlan(queryRequest.Query));
                return;
            }

            var maxItemCount = int.MaxValue;
            var maxItemHeader = context.Request.Headers["x-ms-max-item-count"].ToString();
            if (int.TryParse(maxItemHeader, out var parsedMax) && parsedMax > 0)
                maxItemCount = parsedMax;

            var skip = 0;
            var continuationHeader = context.Request.Headers["x-ms-continuation"].ToString();
            if (!string.IsNullOrEmpty(continuationHeader))
            {
                try
                {
                    skip = BitConverter.ToInt32(Convert.FromBase64String(continuationHeader), 0);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(nameof(CreateDocumentEndpoint), nameof(GetResponse), "Malformed continuation token; starting from beginning: {0}", ex.Message);
                }
            }

            var queryResult = _dataPlane.QueryDocuments(
                context, databaseName, collectionName, queryRequest, maxItemCount, skip);

            switch (queryResult.Result)
            {
                case OperationResult.NotFound:
                    response.StatusCode = HttpStatusCode.NotFound;
                    return;
                case OperationResult.BadRequest:
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.CreateJsonContentResponse(new System.Text.Json.Nodes.JsonObject
                    {
                        ["code"] = "BadRequest",
                        ["message"] = queryResult.Reason ?? "Bad request."
                    });
                    return;
            }

            if (queryResult.Resource!.NextSkip.HasValue)
            {
                var token = Convert.ToBase64String(
                    BitConverter.GetBytes(queryResult.Resource.NextSkip.Value));
                response.Headers.Add("x-ms-continuation", token);
            }

            response.Headers.Add("x-ms-request-charge", "1");
            response.CreateJsonContentResponse(queryResult.Resource);
            return;
        }

        // ── Create-document path ──────────────────────────────────────────────

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
        response.Headers.Add("x-ms-session-token", "0:-1#1");
        response.CreateJsonContentResponse(result.Resource!, HttpStatusCode.Created);
    }
}
