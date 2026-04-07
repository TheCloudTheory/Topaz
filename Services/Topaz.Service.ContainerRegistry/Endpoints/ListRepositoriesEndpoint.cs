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
/// Implements GET /v2/_catalog
///
/// Returns the list of repository names stored in this registry, as defined by the
/// OCI Distribution Spec. The response body is <c>{"repositories":["repo1","repo2",...]}</c>.
/// Supports the optional <c>n</c> (page size) and <c>last</c> (pagination cursor) query parameters.
/// </summary>
internal sealed class ListRepositoriesEndpoint(AcrDataPlane dataPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["GET /v2/_catalog", "GET /acr/v1/_catalog"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListRepositoriesEndpoint), nameof(GetResponse),
            "Executing {0}", nameof(GetResponse));

        var identifiers = ResolveRegistry(context);
        if (identifiers == null)
        {
            response.CreateJsonContentResponse(
                "{\"errors\":[{\"code\":\"NAME_UNKNOWN\",\"message\":\"repository not found\"}]}",
                HttpStatusCode.NotFound);
            return;
        }

        var (sub, rg, registryName) = identifiers.Value;
        var all = dataPlane.ListRepositories(sub, rg, registryName);

        // Optional pagination: ?last=<name>&n=<count>
        var lastParam = context.Request.Query["last"].FirstOrDefault();
        var nParam    = context.Request.Query["n"].FirstOrDefault();

        IEnumerable<string> repos = all;

        if (lastParam is not null)
            repos = repos.SkipWhile(r => string.Compare(r, lastParam, StringComparison.Ordinal) <= 0);

        if (int.TryParse(nParam, out var pageSize) && pageSize > 0)
            repos = repos.Take(pageSize);

        var result = repos.ToList();

        var body = JsonSerializer.Serialize(new { repositories = result }, GlobalSettings.JsonOptions);
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
