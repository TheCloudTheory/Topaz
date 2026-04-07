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
/// Implements PUT /v2/{name}/blobs/uploads/{uuid}?digest=sha256:{hex}
///
/// Finalises a blob upload: optionally appends the request body as the last chunk,
/// verifies the provided digest, moves the blob into the content-addressable store,
/// and returns 201 Created with a Docker-Content-Digest header.
/// Returns 400 if the digest does not match the uploaded content.
/// </summary>
internal sealed class CompleteBlobUploadEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["PUT /v2/{name}/blobs/uploads/{uuid}"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var repository = context.Request.Path.Value.ExtractValueFromPath(2) ?? string.Empty;
        var uuid       = context.Request.Path.Value.ExtractValueFromPath(5) ?? string.Empty;
        var digest     = context.Request.Query["digest"].ToString();

        logger.LogDebug(nameof(CompleteBlobUploadEndpoint), nameof(GetResponse),
            "Executing {0}: repository={1} uuid={2} digest={3}", nameof(GetResponse), repository, uuid, digest);

        if (string.IsNullOrWhiteSpace(digest))
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"DIGEST_INVALID\",\"message\":\"digest query parameter is required\"}]}",
                HttpStatusCode.BadRequest);
            return;
        }

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;

        // Pass the body only when there is actual content — PUT may carry a final monolithic body
        // or may have an empty body when the data was already sent via PATCH chunks.
        Stream? finalChunk = context.Request.ContentLength is > 0 ? context.Request.Body : null;

        try
        {
            var verifiedDigest = dataPlane.CompleteUpload(sub, rg, registryName, uuid, digest, finalChunk);
            if (verifiedDigest == null)
            {
                response.CreateJsonContentResponse(
                    "{\"errors\":[{\"code\":\"DIGEST_INVALID\",\"message\":\"provided digest does not match uploaded content\"}]}",
                    HttpStatusCode.BadRequest);
                return;
            }

            response.Headers.Add("Docker-Content-Digest", verifiedDigest);
            response.Headers.Location = new Uri($"https://{context.Request.Host}/v2/{repository}/blobs/{verifiedDigest}");
            response.CreateJsonContentResponse("{}", HttpStatusCode.Created);
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
