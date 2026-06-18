using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Collections;

internal sealed class CreateCollectionEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public CreateCollectionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private CreateCollectionEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["POST /dbs/{db}/colls"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];

        var body = JsonSerializer.Deserialize<SqlContainerInnerResource>(
            context.Request.Body, GlobalSettings.JsonOptions);

        var collectionName = body?.Id;
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        int? throughput = null;
        if (context.Request.Headers.TryGetValue("x-ms-offer-throughput", out var throughputHeader)
            && int.TryParse(throughputHeader.ToString(), out var parsed))
        {
            throughput = parsed;
        }

        var result = _dataPlane.CreateCollection(
            context, databaseName, collectionName,
            throughput, body?.PartitionKey, body?.IndexingPolicy, body?.UniqueKeyPolicy, body?.DefaultTtl);

        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        if (result.Result == OperationResult.Updated)
        {
            response.StatusCode = HttpStatusCode.Conflict;
            return;
        }

        response.Headers.Add("x-ms-request-charge", "1");
        response.CreateJsonContentResponse(result.Resource, HttpStatusCode.Created);
    }
}
