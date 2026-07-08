using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Collections;

internal sealed class GetCollectionEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public GetCollectionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), eventPipeline, logger) { }

    private GetCollectionEndpoint(CosmosDbDataPlane dataPlane, Pipeline eventPipeline, ITopazLogger logger)
        : base(dataPlane, eventPipeline, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["GET /dbs/{db}/colls/{coll}"];
    public override string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/read"];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];

        var result = _dataPlane.GetCollection(context, databaseName, collectionName);
        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Headers.Add("x-ms-request-charge", "1");
        response.CreateJsonContentResponse(result.Resource);
    }
}
