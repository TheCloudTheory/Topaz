using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints;

/// <summary>
/// Implements HEAD /v2/{name}/manifests/{reference}
///
/// Returns 200 with Docker-Content-Digest, Content-Type, and Content-Length when the
/// manifest exists, without a response body. Returns 404 when not found.
/// </summary>
internal sealed class HeadManifestEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["HEAD /v2/{name}/manifests/{reference}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var name      = context.Request.Path.Value.ExtractValueFromPath(2) ?? string.Empty;
        var reference = context.Request.Path.Value.ExtractValueFromPath(4) ?? string.Empty;

        logger.LogDebug(nameof(HeadManifestEndpoint), nameof(GetResponse),
            "Executing {0}: name={1} reference={2}", nameof(GetResponse), name, reference);

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;
        var envelope = dataPlane.GetManifest(sub, rg, registryName, name, reference);

        if (envelope == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"MANIFEST_UNKNOWN\",\"message\":\"manifest not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        response.Headers.Add("Docker-Content-Digest", envelope.Digest);
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(envelope.ContentType);
        response.Content.Headers.ContentLength = 0;
        response.StatusCode = HttpStatusCode.OK;
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
