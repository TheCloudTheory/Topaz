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
/// Implements PATCH /v2/{name}/blobs/uploads/{uuid}
///
/// Appends a chunk to an in-progress blob upload session and returns 202 with an
/// updated Range header indicating how many bytes have been received.
/// </summary>
internal sealed class PatchBlobUploadEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["PATCH /v2/{name}/blobs/uploads/{uuid}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var repository = context.Request.Path.Value.ExtractValueFromPath(2) ?? string.Empty;
        var uuid       = context.Request.Path.Value.ExtractValueFromPath(5) ?? string.Empty;

        logger.LogDebug(nameof(PatchBlobUploadEndpoint), nameof(GetResponse),
            "Executing {0}: repository={1} uuid={2}", nameof(GetResponse), repository, uuid);

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;

        try
        {
            var (start, end) = dataPlane.AppendChunk(sub, rg, registryName, uuid, context.Request.Body);
            response.Headers.Location = new Uri($"https://{context.Request.Host}/v2/{repository}/blobs/uploads/{uuid}");
            response.Headers.Add("Range", $"{start}-{end}");
            response.CreateJsonContentResponse("{}", HttpStatusCode.Accepted);
        }
        catch (FileNotFoundException)
        {
            response.CreateJsonContentResponse(
                $"{{\"errors\":[{{\"code\":\"BLOB_UPLOAD_UNKNOWN\",\"message\":\"upload session '{uuid}' not found\"}}]}}",
                HttpStatusCode.NotFound);
        }
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
