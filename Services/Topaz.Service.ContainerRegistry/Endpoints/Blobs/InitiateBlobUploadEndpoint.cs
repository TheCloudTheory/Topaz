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
/// Implements POST /v2/{name}/blobs/uploads/
///
/// Initiates a new blob upload session and returns a 202 Accepted with:
///   Location: /v2/{name}/blobs/uploads/{uuid}
///   Range: 0-0
/// as required by the OCI Distribution Specification.
/// </summary>
internal sealed class InitiateBlobUploadEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["POST /v2/{name}/blobs/uploads/"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var repository = context.Request.Path.Value.ExtractValueFromPath(2) ?? string.Empty;

        logger.LogDebug(nameof(InitiateBlobUploadEndpoint), nameof(GetResponse),
            "Executing {0}: repository={1}", nameof(GetResponse), repository);

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;
        var uuid = dataPlane.InitiateUpload(sub, rg, registryName);

        response.Headers.Location = new Uri($"https://{context.Request.Host}/v2/{repository}/blobs/uploads/{uuid}");
        response.Headers.Add("OCI-Chunk-Min-Length", "0");
        response.Headers.Add("Range", "0-0");
        response.CreateJsonContentResponse("{}", HttpStatusCode.Accepted);
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
