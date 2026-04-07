using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints;

/// <summary>
/// Implements PUT /v2/{name}/manifests/{reference}
///
/// Stores an OCI or Docker image manifest, keyed by tag or digest.
/// Returns 201 Created with a Docker-Content-Digest header containing the manifest's SHA-256 digest.
/// </summary>
internal sealed class PutManifestEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["PUT /v2/{name}/manifests/{reference}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var name      = context.Request.Path.Value.ExtractValueFromPath(2) ?? string.Empty;
        var reference = context.Request.Path.Value.ExtractValueFromPath(4) ?? string.Empty;

        logger.LogDebug(nameof(PutManifestEndpoint), nameof(GetResponse),
            "Executing {0}: name={1} reference={2}", nameof(GetResponse), name, reference);

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        using var ms = new MemoryStream();
        context.Request.Body.CopyTo(ms);
        var manifestBytes = ms.ToArray();

        var contentType = context.Request.ContentType
            ?? "application/vnd.docker.distribution.manifest.v2+json";

        var (sub, rg, registryName) = identifiers.Value;
        var digest = dataPlane.PutManifest(sub, rg, registryName, name, reference, manifestBytes, contentType);

        response.Headers.Add("Docker-Content-Digest", digest);
        response.Headers.Location = new Uri($"https://{context.Request.Host}/v2/{name}/manifests/{digest}");
        response.CreateJsonContentResponse("{}", HttpStatusCode.Created);
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
