using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics.Endpoints.DataPlane;

internal sealed class DataCollectionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly LogAnalyticsDataPlane _dataPlane = LogAnalyticsDataPlane.New(eventPipeline, logger);
    
    public string[] Endpoints => ["POST /api/logs"];
    public string[] Permissions => [];
    public string? RequiredHostServiceLabel => "ods.opinsights";

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var workspaceId = context.Request.Host.Host.Split('.')[0];
        var body = new StreamReader(context.Request.Body).ReadToEnd();
        
        var result = _dataPlane.SaveIngestedData(workspaceId, context.Request.Headers["Log-Type"].FirstOrDefault(), body);
        if (result.Result != OperationResult.Success)
        {
            logger.LogError(nameof(DataCollectionEndpoint), nameof(GetResponse), "Failed to save data: {0}", result.Reason);
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(string.Empty);
    }
}