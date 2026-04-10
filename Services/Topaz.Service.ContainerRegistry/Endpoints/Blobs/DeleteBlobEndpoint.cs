using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Blobs;

/// <summary>
/// Implements DELETE /v2/{name}/blobs/{digest}
///
/// Deletes a blob by digest. Returns 202 Accepted on success,
/// 404 Not Found when the blob or registry does not exist.
/// </summary>
internal sealed class DeleteBlobEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["DELETE /v2/{name}/blobs/{digest}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var digest = context.Request.Path.Value.ExtractValueFromPath(4) ?? string.Empty;

        logger.LogDebug(nameof(DeleteBlobEndpoint), nameof(GetResponse),
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
        var deleted = dataPlane.DeleteBlob(sub, rg, registryName, digest);

        if (!deleted)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"BLOB_UNKNOWN\",\"message\":\"blob not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        response.StatusCode = HttpStatusCode.Accepted;
        response.Content = new StringContent(string.Empty);
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
