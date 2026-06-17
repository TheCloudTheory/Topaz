using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Databases;

internal sealed class CreateDatabaseEndpoint : CosmosDataPlaneEndpointBase
{
    private readonly CosmosDbDataPlane _dataPlane;

    public CreateDatabaseEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    private CreateDatabaseEndpoint(CosmosDbDataPlane dataPlane, ITopazLogger logger)
        : base(dataPlane, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["POST /dbs"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!IsRequestAuthorized(context, response)) return;

        var body = JsonSerializer.Deserialize<CreateDatabaseRequest>(
            context.Request.Body, GlobalSettings.JsonOptions);

        var databaseName = body?.Id;
        if (string.IsNullOrWhiteSpace(databaseName))
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

        var result = _dataPlane.CreateDatabase(context, databaseName, throughput);
        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.StatusCode = result.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.Headers.Add("x-ms-request-charge", "1");
        response.CreateJsonContentResponse(result.Resource);
    }

    private sealed class CreateDatabaseRequest
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
