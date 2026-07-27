using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Redis.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Redis.Endpoints;

internal sealed class RegenerateRedisKeyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly RedisServiceControlPlane _controlPlane =
        RedisServiceControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Cache";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{sub}/resourceGroups/{rg}/providers/Microsoft.Cache/redis/{name}/regenerateKey"
    ];

    public string[] Permissions => ["Microsoft.Cache/redis/regenerateKey/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);

        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<RegenerateRedisKeyRequest>(
            reader.ReadToEnd(), GlobalSettings.JsonOptions);

        if (string.IsNullOrWhiteSpace(request?.KeyType))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.RegenerateKey(sub, rg, name!, request.KeyType);
        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!, HttpStatusCode.NotFound);
            return;
        }

        response.CreateJsonContentResponse(result.Resource);
    }
}
