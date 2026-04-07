using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.ContainerRegistry.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints;

/// <summary>
/// Implements GET /v2/{name}/tags/list
///
/// Returns the sorted list of tags for a repository as defined by the OCI Distribution Spec.
/// The response body is <c>{"name":"{repository}","tags":["tag1","tag2",...]}</c>.
/// Supports the optional <c>n</c> (page size) and <c>last</c> (pagination cursor) query parameters.
/// </summary>
internal sealed class ListTagsEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["GET /v2/{name}/tags/list"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var repository = context.Request.Path.Value.ExtractValueFromPath(2) ?? string.Empty;

        logger.LogDebug(nameof(ListTagsEndpoint), nameof(GetResponse),
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
        var all = dataPlane.ListTags(sub, rg, registryName, repository);

        // Optional pagination: ?last=<tag>&n=<count>
        var lastParam = context.Request.Query["last"].FirstOrDefault();
        var nParam    = context.Request.Query["n"].FirstOrDefault();

        IEnumerable<string> tags = all;

        if (lastParam is not null)
            tags = tags.SkipWhile(t => string.Compare(t, lastParam, StringComparison.Ordinal) <= 0);

        if (int.TryParse(nParam, out var pageSize) && pageSize > 0)
            tags = tags.Take(pageSize);

        var tagList = tags.ToList();

        var body = JsonSerializer.Serialize(
            new { name = repository, tags = tagList.Count > 0 ? tagList : null },
            GlobalSettings.JsonOptions);

        response.CreateJsonContentResponse(body, HttpStatusCode.OK);
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
