using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Blobs;

/// <summary>
/// Implements HEAD /v2/{name}/blobs/{digest}
///
/// Docker sends this after completing a blob upload (and before pushing a manifest) to
/// confirm the blob is stored and addressable. Returns 200 with Content-Length and
/// Docker-Content-Digest, or 404 when the blob is not found.
/// </summary>
internal sealed class HeadBlobEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["HEAD /v2/{name}/blobs/{digest}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var digest = context.Request.Path.Value.ExtractValueFromPath(4) ?? string.Empty;

        logger.LogDebug(nameof(HeadBlobEndpoint), nameof(GetResponse),
            "Executing {0}: digest={1}", nameof(GetResponse), digest);

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;
        var blob = dataPlane.GetBlob(sub, rg, registryName, digest);

        if (blob == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"BLOB_UNKNOWN\",\"message\":\"blob not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        response.Headers.Add("Docker-Content-Digest", digest);
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentLength = 0;
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
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
