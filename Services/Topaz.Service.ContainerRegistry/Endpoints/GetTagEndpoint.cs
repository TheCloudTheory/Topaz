using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints;

/// <summary>
/// Implements GET /acr/v1/{name}/_tags/{tag}
///
/// Returns ACR v1 tag metadata, including digest, used by Azure CLI repository delete flows.
/// </summary>
internal sealed class GetTagEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["GET /acr/v1/{name}/_tags/{tag}", "GET /acr/v1/{name}/_tags/{tag}/"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var repository = context.Request.Path.Value.ExtractValueFromPath(3) ?? string.Empty;
        var tag = context.Request.Path.Value.ExtractValueFromPath(5) ?? string.Empty;

        logger.LogDebug(nameof(GetTagEndpoint), nameof(GetResponse),
            "Executing {0}: repository={1} tag={2}", nameof(GetResponse), repository, tag);

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;
        var manifest = dataPlane.GetManifest(sub, rg, registryName, repository, tag);
        if (manifest == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"MANIFEST_UNKNOWN\",\"message\":\"manifest not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var body = JsonSerializer.Serialize(
            new
            {
                registry = $"{registryName}.azurecr.io",
                imageName = repository,
                tag = new
                {
                    name = tag,
                    digest = manifest.Digest
                }
            },
            GlobalSettings.JsonOptions);

        response.CreateJsonContentResponse(body, HttpStatusCode.OK);
    }

    private static (SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string registryName)?
        ResolveRegistry(HttpContext context)
    {
        var registryName = context.Request.Host.Host.Split('.')[0];
        var identifiers = GlobalDnsEntries.GetEntry(ContainerRegistryService.UniqueName, registryName);
        if (identifiers == null) return null;
        return (SubscriptionIdentifier.From(identifiers.Value.subscription),
                ResourceGroupIdentifier.From(identifiers.Value.resourceGroup!),
                registryName);
    }
}
