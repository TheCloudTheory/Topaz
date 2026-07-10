using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.LogAnalytics.Endpoints.DataPlane;

internal sealed class DataCollectionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly LogAnalyticsDataPlane _dataPlane = LogAnalyticsDataPlane.New(eventPipeline, logger);
    
    public string[] Endpoints => ["/api/logs"];
    public string[] Permissions => [];
    public string? RequiredHostServiceLabel => "ods.opinsights";

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var workspaceId = context.Request.Host.Host.Split('.')[0];
        var logType = context.Request.Headers["Log-Type"].FirstOrDefault() ?? "CustomLog";
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var id = Guid.NewGuid().ToString();

        var body = new StreamReader(context.Request.Body).ReadToEnd();
        var records = JsonSerializer.Deserialize<JsonElement[]>(body) ?? [];

        var dir = Path.Combine(GlobalSettings.MainEmulatorDirectory, "log-analytics", workspaceId, logType, date);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{id}.json"), JsonSerializer.Serialize(records));
        
        _dataPlane.SaveIngestedData(workspaceId, context.Request.Headers["Log-Type"].FirstOrDefault(), body);

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(string.Empty);
    }
}