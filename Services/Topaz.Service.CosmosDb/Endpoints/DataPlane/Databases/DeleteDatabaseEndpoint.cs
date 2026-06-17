using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Databases;

internal sealed class DeleteDatabaseEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public DeleteDatabaseEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private DeleteDatabaseEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["DELETE /dbs/{db}"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];

        var result = _dataPlane.DeleteDatabase(context, databaseName);

        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
        response.Headers.Add("x-ms-request-charge", "1");
    }
}
