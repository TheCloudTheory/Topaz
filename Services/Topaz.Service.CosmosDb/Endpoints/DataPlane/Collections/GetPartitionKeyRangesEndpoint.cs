using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.CosmosDb.Endpoints.DataPlane.Collections;

/// <summary>
/// Returns a single partition key range covering all data.
/// The Cosmos SDK uses this endpoint for cross-partition query planning.
/// Topaz emulates a single-partition container, so we always return one range.
/// </summary>
internal sealed class GetPartitionKeyRangesEndpoint : CosmosDataPlaneEndpointBase
{
    public GetPartitionKeyRangesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : base(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), logger) { }

    public override string[] Endpoints => ["GET /dbs/{dbRid}/colls/{collRid}/pkranges"];
    public override string[] Permissions => [];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        Console.Error.WriteLine($"[DBG-PKR] {context.Request.Path}");
        // Auth is intentionally skipped for pkranges: the Cosmos SDK signs this request
        // using the internal collection RID as the resource link, which Topaz cannot
        // reconstruct from the request path alone. pkranges returns only structural
        // metadata (partition boundaries) and contains no sensitive user data.
        // See: known-limitations.md — "Cosmos DB — pkranges auth bypass"
        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var collRid = segments.Length > 3 ? segments[3] : string.Empty;

        var result = new JsonObject
        {
            ["_rid"] = collRid,
            ["PartitionKeyRanges"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "0",
                    ["_rid"] = "0",
                    ["minInclusive"] = "",
                    ["maxExclusive"] = "FF",
                    ["_lsn"] = 1,
                    ["_self"] = $"colls/{collRid}/pkranges/0/",
                    ["_etag"] = "\"topaz-pkrange\""
                }
            },
            ["_count"] = 1
        };

        response.Headers.Add("x-ms-request-charge", "1");
        response.CreateJsonContentResponse(result);
    }
}
