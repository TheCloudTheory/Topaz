using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.ServicePrincipal;

public class ListServicePrincipalsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServicePrincipalDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "GET /v1.0/servicePrincipals",
        "GET /beta/servicePrincipals",
        "GET /servicePrincipals",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListServicePrincipalsEndpoint), nameof(GetResponse), "Fetching service principals.");

        var operation = _dataPlane.ListServicePrincipals();
        var principals = operation.Resource ?? [];

        // Apply $filter=appId eq 'xxx' when provided (used by azuread Terraform provider)
        if (context.Request.Query.TryGetValue("$filter", out var filterValue))
        {
            var filter = filterValue.ToString();
            var appIdFilter = ParseAppIdFilter(filter);
            if (appIdFilter != null)
            {
                principals = principals
                    .Where(sp => string.Equals(sp.AppId, appIdFilter, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }
        }

        response.CreateJsonContentResponse(ServicePrincipalsListResponse.From(principals));
    }

    private static string? ParseAppIdFilter(string filter)
    {
        // Matches: appId eq 'some-guid'
        var match = System.Text.RegularExpressions.Regex.Match(
            filter,
            @"appId\s+eq\s+'([^']+)'",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }
}