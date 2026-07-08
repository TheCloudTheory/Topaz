using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Collections;

internal sealed class ListCollectionsEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public ListCollectionsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), eventPipeline, logger) { }

    private ListCollectionsEndpoint(CosmosDbDataPlane dataPlane, Pipeline eventPipeline, ITopazLogger logger)
        : base(dataPlane, eventPipeline, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["GET /dbs/{db}/colls"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];

        var ctx = _dataPlane.ResolveAccountContext(context);
        if (ctx == null) { response.StatusCode = HttpStatusCode.NotFound; return; }

        var result = _dataPlane.ListCollections(ctx, databaseName);
        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Headers.Add("x-ms-request-charge", "1");
        response.CreateJsonContentResponse(DataPlaneListCollectionsResponse.From(result.Resource));
    }
}
