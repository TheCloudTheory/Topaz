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
/// Implements DELETE /acr/v1/{name}
///
/// Deletes a repository and all manifests for that repository in the ACR v1 data-plane API.
/// </summary>
internal sealed class DeleteRepositoryEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["DELETE /acr/v1/{name}", "DELETE /acr/v1/{name}/"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var name = context.Request.Path.Value.ExtractValueFromPath(3) ?? string.Empty;

        logger.LogDebug(nameof(DeleteRepositoryEndpoint), nameof(GetResponse),
            "Executing {0}: name={1}", nameof(GetResponse), name);

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;
        var deleted = dataPlane.DeleteRepository(sub, rg, registryName, name);

        if (!deleted)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
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
