using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Tags;

/// <summary>
/// Implements DELETE /acr/v1/{name}/_tags/{tag}
///
/// Deletes a single image tag in the ACR v1 data-plane API.
/// Internally this removes the manifest reference by tag.
/// </summary>
internal sealed class DeleteTagEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["DELETE /acr/v1/{name}/_tags/{tag}", "DELETE /acr/v1/{name}/_tags/{tag}/"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var name = context.Request.Path.Value.ExtractValueFromPath(3) ?? string.Empty;
        var tag = context.Request.Path.Value.ExtractValueFromPath(5) ?? string.Empty;

        logger.LogDebug(nameof(DeleteTagEndpoint), nameof(GetResponse),
            "Executing {0}: name={1} tag={2}", nameof(GetResponse), name, tag);

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;
        var deleted = dataPlane.DeleteManifest(sub, rg, registryName, name, tag);

        if (!deleted)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"MANIFEST_UNKNOWN\",\"message\":\"manifest not found\"}]}",
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
