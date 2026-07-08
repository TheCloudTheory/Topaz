using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Collections;

internal sealed class DeleteCollectionEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public DeleteCollectionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), eventPipeline, logger) { }

    private DeleteCollectionEndpoint(CosmosDbDataPlane dataPlane, Pipeline eventPipeline, ITopazLogger logger)
        : base(dataPlane, eventPipeline, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["DELETE /dbs/{db}/colls/{coll}"];
    public override string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/delete"];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];

        var result = _dataPlane.DeleteCollection(context, databaseName, collectionName);

        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
        response.Headers.Add("x-ms-request-charge", "1");
    }
}
