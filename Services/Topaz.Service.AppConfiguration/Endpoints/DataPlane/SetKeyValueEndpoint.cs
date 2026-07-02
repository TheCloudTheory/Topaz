using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.AppConfiguration.Endpoints.DataPlane;

internal sealed class SetKeyValueEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : AppConfigurationDataPlaneEndpointBase(eventPipeline, logger)
{
    public override string[] Endpoints => ["PUT /kv/{key}"];

    public override void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ctx = GetStoreContext(context);
        var key = Uri.UnescapeDataString(context.Request.Path.Value!.Split('/').Last());
        var label = context.Request.Query["label"].ToString();
        var labelOrNull = string.IsNullOrEmpty(label) ? null : label;

        // Check if the KV is locked
        var existing = ControlPlane.GetKv(ctx.Sub, ctx.Rg, ctx.StoreName, key, labelOrNull);
        if (existing is { Locked: true })
        {
            response.StatusCode = HttpStatusCode.Conflict;
            response.Content = new System.Net.Http.StringContent(
                """{"type":"https://azconfig.io/errors/key-locked","title":"The key is read-only."}""",
                System.Text.Encoding.UTF8, "application/json");
            return;
        }

        using var reader = new System.IO.StreamReader(context.Request.Body);
        var body = JsonSerializer.Deserialize<SetKeyValueRequest>(reader.ReadToEnd(), GlobalSettings.JsonOptions);

        var kv = ControlPlane.SetKv(ctx.Sub, ctx.Rg, ctx.StoreName, key, labelOrNull,
            body?.Value, body?.ContentType, body?.Tags);

        response.CreateJsonContentResponse(kv);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"{kv.Etag}\"");
        response.Content.Headers.LastModified = kv.LastModified;
        response.StatusCode = HttpStatusCode.OK;
    }

    private sealed class SetKeyValueRequest
    {
        [JsonPropertyName("value")] public string? Value { get; set; }
        [JsonPropertyName("content_type")] public string? ContentType { get; set; }
        [JsonPropertyName("tags")] public Dictionary<string, string>? Tags { get; set; }
    }
}
