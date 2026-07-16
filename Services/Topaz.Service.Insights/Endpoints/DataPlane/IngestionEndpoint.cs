using System.Net;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Insights.Endpoints.DataPlane;

internal sealed class IngestionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ApplicationInsightsServiceControlPlane
        _controlPlane = ApplicationInsightsServiceControlPlane.New(eventPipeline, logger);
    private readonly ApplicationInsightsDataPlane _dataPlane = ApplicationInsightsDataPlane.New;
    private string? _requestContent;
    
    public string[] Endpoints => ["POST /v2/track", "POST /v2.1/track"];
    public string[] Permissions => EndpointPermissions.None;
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public string? RequiredHostServiceLabel => "applicationinsights";
    
    public (bool isAuthorized, ClaimsPrincipal? principal) Authorize(HttpContext context,
        HttpResponseMessage response,
        IArmAuthorizationChecker armAuthChecker)
    {
        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        _requestContent = content;
        
        // Application Insights SDK sends data as NDJSON
        var firstLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var ikey = JsonNode.Parse(firstLine)?["iKey"]?.ToString();

        if (string.IsNullOrWhiteSpace(ikey))
        {
            logger.LogDebug(nameof(IngestionEndpoint), nameof(Authorize), "Instrumentation key is null or empty.");
            return (false, null);
        }

        var operation = _controlPlane.GetByInstrumentationKey(ikey);
        if (operation is { Result: OperationResult.Success, Resource: not null })
        {
            return (true, null);
        }
        
        logger.LogDebug(nameof(IngestionEndpoint), nameof(Authorize), "There is no component for instrumentation key {0}.", ikey);
        return (false, null);
    }

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var content = _requestContent ?? string.Empty;
        var firstLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        var json = JsonNode.Parse(firstLine);
        var instrumentationKey = json?["iKey"]?.ToString();
        var type = json?["data"]?["baseType"]?.ToString();
        if (json == null || string.IsNullOrWhiteSpace(instrumentationKey) ||  string.IsNullOrWhiteSpace(type))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }
        
        var result = _dataPlane.Ingest(instrumentationKey, type, content);
        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }
        
        response.CreateJsonContentResponse(result.Resource!);
    }
}