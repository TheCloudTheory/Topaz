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
/// Implements DELETE /v2/{name}/manifests/{reference}
///
/// Deletes a manifest by tag or digest. Returns 202 Accepted on success,
/// 404 Not Found when the manifest or registry does not exist.
/// </summary>
internal sealed class DeleteManifestEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["DELETE /v2/{name}/manifests/{reference}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var name      = context.Request.Path.Value.ExtractValueFromPath(2) ?? string.Empty;
        var reference = context.Request.Path.Value.ExtractValueFromPath(4) ?? string.Empty;

        logger.LogDebug(nameof(DeleteManifestEndpoint), nameof(GetResponse),
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
        var deleted = dataPlane.DeleteManifest(sub, rg, registryName, name, reference);

        if (!deleted)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"MANIFEST_UNKNOWN\",\"message\":\"manifest not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        response.StatusCode = HttpStatusCode.Accepted;
        response.Content = new System.Net.Http.StringContent(string.Empty);
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
