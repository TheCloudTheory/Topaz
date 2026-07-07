using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Documents;

internal sealed class ListDocumentsEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public ListDocumentsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private ListDocumentsEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["GET /dbs/{db}/colls/{coll}/docs"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var databaseName = segments[1];
        var collectionName = segments[3];

        var ctx = _dataPlane.ResolveAccountContext(context);
        if (ctx == null) { response.StatusCode = HttpStatusCode.NotFound; return; }

        var result = _dataPlane.ListDocuments(ctx, databaseName, collectionName);

        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var listResponse = DataPlaneListDocumentsResponse.From(result.Resource ?? []);
        response.Headers.Add("x-ms-request-charge", "1");
        response.CreateJsonContentResponse(listResponse);
    }
}
