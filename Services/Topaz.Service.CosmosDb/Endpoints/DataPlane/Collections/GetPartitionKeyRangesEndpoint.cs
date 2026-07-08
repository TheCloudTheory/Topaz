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
    private readonly CosmosDbDataPlane _dataPlane;

    public GetPartitionKeyRangesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
        : this(new CosmosDbDataPlane(new DatabaseAccountResourceProvider(logger), logger), eventPipeline, logger) { }

    private GetPartitionKeyRangesEndpoint(CosmosDbDataPlane dataPlane, Pipeline eventPipeline, ITopazLogger logger)
        : base(dataPlane, eventPipeline, logger)
    {
        _dataPlane = dataPlane;
    }

    public override string[] Endpoints => ["GET /dbs/{dbRid}/colls/{collRid}/pkranges"];
    public override string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/read"];
    public override string? ProviderNamespace => null;

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        // Auth is intentionally skipped for pkranges: the Cosmos SDK signs this request
        // using the internal collection RID as the resource link, which Topaz cannot
        // reconstruct from the request path alone. pkranges returns only structural
        // metadata (partition boundaries) and contains no sensitive user data.
        // See: known-limitations.md — "Cosmos DB — pkranges auth bypass"

        // The Cosmos SDK fetches pkranges via an incremental change-feed loop:
        //   1st call: no If-None-Match  → server returns 200 with ranges + ETag
        //   2nd call: If-None-Match set → server returns 304 Not Modified → loop ends
        // Without 304, the loop runs forever and the actual document GET is never made.
        const string pkrangeEtag = "\"topaz-pkrange\"";
        var ifNoneMatch = context.Request.Headers["If-None-Match"].ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == pkrangeEtag)
        {
            response.StatusCode = System.Net.HttpStatusCode.NotModified;
            response.Headers.Add("ETag", pkrangeEtag);
            return;
        }

        var segments = context.Request.Path.Value!.Trim('/').Split('/');
        var collRidSegment = segments.Length > 3 ? segments[3] : string.Empty;

        // The Cosmos SDK sometimes sends pkranges requests using only the first 4 bytes of
        // the collection _rid as the URL segment (a truncated form from ResourceId parsing).
        // We look up the real collection so the response _rid matches the collection._rid
        // that the SDK uses as its routing-map cache key.
        var collection = _dataPlane.GetCollectionByRidPrefix(context, collRidSegment);
        var actualRid = collection?.Rid ?? collRidSegment;

        var result = new JsonObject
        {
            ["_rid"] = actualRid,
            ["PartitionKeyRanges"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "0",
                    ["_rid"] = "0",
                    ["minInclusive"] = "",
                    ["maxExclusive"] = "FF",
                    ["_lsn"] = 1,
                    ["_self"] = $"colls/{actualRid}/pkranges/0/",
                    ["_etag"] = pkrangeEtag,
                    ["status"] = "online",
                    ["parents"] = new JsonArray()
                }
            },
            ["_count"] = 1
        };

        response.Headers.Add("x-ms-request-charge", "1");
        response.Headers.Add("ETag", pkrangeEtag);
        response.CreateJsonContentResponse(result);
    }
}
