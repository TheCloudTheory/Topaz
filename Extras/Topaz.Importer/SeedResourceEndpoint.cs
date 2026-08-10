using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Importer;

internal sealed class SeedResourceEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AzureImporterService _importerService = new(eventPipeline, logger);
    
    public string[] Endpoints => ["POST /topaz/extras/seed"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public string? ProviderNamespace => "Topaz";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
    }

    public async Task GetResponseAsync(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<SeedResourcesRequest>(await reader.ReadToEndAsync(), GlobalSettings.JsonOptions);

        if (request == null || string.IsNullOrWhiteSpace(request.SubscriptionId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = await _importerService.Import(SubscriptionIdentifier.From(request.SubscriptionId),
            ResourceGroupIdentifier.From(request.ResourceGroup), request.ResourceType, request.DryRun,
            request.Overwrite);
        
        response.CreateJsonContentResponse(result);
    }
}