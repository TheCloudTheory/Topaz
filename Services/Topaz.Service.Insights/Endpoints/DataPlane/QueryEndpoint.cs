using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights.Endpoints.DataPlane;

internal sealed class QueryEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ApplicationInsightsServiceControlPlane _controlPlane =
        ApplicationInsightsServiceControlPlane.New(eventPipeline, logger);
    private readonly ApplicationInsightsDataPlane _dataPlane =
        ApplicationInsightsDataPlane.New(eventPipeline, logger);
    
    public string[] Endpoints => ["POST /v1/apps/{instrumentationKey}/query"];
    public string[] Permissions => EndpointPermissions.None;
    public string? RequiredHostServiceLabel => "applicationinsights";

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(
        HttpContext context, HttpResponseMessage response, IArmAuthorizationChecker armAuthChecker)
    {
        var ikey = ExtractInstrumentationKey(context);
        if (string.IsNullOrWhiteSpace(ikey))
        {
            logger.LogDebug(nameof(QueryEndpoint), nameof(Authorize), "Instrumentation key missing from path.");
            return (false, null);
        }

        var op = _controlPlane.GetByInstrumentationKey(ikey);
        if (op is { Result: OperationResult.Success, Resource: not null })
            return (true, null);

        logger.LogDebug(nameof(QueryEndpoint), nameof(Authorize),
            "No component found for instrumentation key {0}.", ikey);
        return (false, null);
    }

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ikey = ExtractInstrumentationKey(context);
        if (string.IsNullOrWhiteSpace(ikey))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        string body;
        using (var reader = new StreamReader(context.Request.Body))
            body = reader.ReadToEnd();

        string? queryText = null;
        try
        {
            var node = JsonNode.Parse(body);
            queryText = node?["query"]?.GetValue<string>();
        }
        catch (JsonException ex)
        {
            logger.LogDebug(nameof(QueryEndpoint), nameof(GetResponse), "Failed to parse query body: {0}", ex.Message);
        }

        if (string.IsNullOrWhiteSpace(queryText))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _dataPlane.Query(ikey, queryText);
        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var wire = new
        {
            tables = result.Resource.Tables.Select(t => new
            {
                name = t.Name,
                columns = t.Columns.Select(c => new { name = c.Name, type = c.Type }).ToArray(),
                rows = t.Rows
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(wire, GlobalSettings.JsonOptions);
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(json);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    private static string? ExtractInstrumentationKey(HttpContext context)
    {
        // Path: /v1/apps/{instrumentationKey}/query
        var segments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // segments: ["v1", "apps", "{ikey}", "query"]
        return segments?.Length >= 3 ? segments[2] : null;
    }
}
