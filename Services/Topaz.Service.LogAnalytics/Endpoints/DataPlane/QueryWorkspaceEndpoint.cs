using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.LogAnalytics.Endpoints.DataPlane;

internal sealed class QueryWorkspaceEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly LogAnalyticsDataPlane _dataPlane = LogAnalyticsDataPlane.New(eventPipeline, logger);
    
    public string[] Endpoints => ["POST /v1/workspaces/{workspaceId}/query"];
    public string[] Permissions => [];
    public string? RequiredHostServiceLabel => "api.loganalytics";

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var workspaceId = context.Request.Path.Value.ExtractValueFromPath(3);
        var body = new StreamReader(context.Request.Body).ReadToEnd();
        if(string.IsNullOrWhiteSpace(body))
        {
            logger.LogDebug(nameof(QueryWorkspaceEndpoint), nameof(GetResponse), "No body provided");
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        logger.LogDebug(nameof(QueryWorkspaceEndpoint), nameof(GetResponse), "Received query: {0}", body);

        var query = JsonNode.Parse(body)?["query"]?.ToString();
        if (string.IsNullOrWhiteSpace(query))
        {
            logger.LogDebug(nameof(QueryWorkspaceEndpoint), nameof(GetResponse), "No query provided");
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        var result = _dataPlane.QueryData(workspaceId, query);
        if (result.Result != OperationResult.Success)
        {
            logger.LogError(nameof(QueryWorkspaceEndpoint), nameof(GetResponse), "Failed to query data: {0}", result.Reason);
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(string.Empty);
    }
}