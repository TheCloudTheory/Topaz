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
/// Implements GET /v2/{name}/blobs/{digest}
///
/// Returns the raw binary content of a stored blob, identified by its SHA-256 digest.
/// Used by <c>docker pull</c> to download image layers and config blobs.
/// Returns 404 when the blob does not exist.
/// </summary>
internal sealed class GetBlobEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["GET /v2/{name}/blobs/{digest}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var digest = context.Request.Path.Value.ExtractValueFromPath(4) ?? string.Empty;

        logger.LogDebug(nameof(GetBlobEndpoint), nameof(GetResponse),
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
        response.Content = new ByteArrayContent(blob);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        response.Content.Headers.ContentLength = blob.Length;
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
