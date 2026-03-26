using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Authorization.Models;
using Topaz.Service.Authorization.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Authorization.Endpoints.RoleDefinitions;

public class ListRoleDefinitionsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly AuthorizationControlPlane _controlPlane = AuthorizationControlPlane.New(eventPipeline, logger);
    private const int DefaultPageSize = 10;

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions",
        "GET /{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions",
    ];

    public string[] Permissions => ["Microsoft.Authorization/roleDefinitions/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = context.Request.Path.Value.StartsWith("/subscriptions")
            ? SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2))
            : SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(1));

        logger.LogDebug(nameof(ListRoleDefinitionsEndpoint), nameof(GetResponse),
            "Attempting to list role definitions for subscription ID `{0}` and query `{1}`.",
            subscriptionIdentifier, context.Request.QueryString);

        string? roleName = null;
        if (context.Request.QueryString.TryGetValueForKey("$filter", out var filter))
        {
            // A filter is basically an expression looking like this: $filter=roleName eq 'Contributor'
            roleName = ExtractRoleNamerFromFilter(filter);
        }

        var definitions = _controlPlane.ListRoleDefinitionsBySubscription(subscriptionIdentifier, roleName);
        if (definitions.Result != OperationResult.Success || definitions.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var orderedDefinitions = definitions.Resource
            .OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var pageSize = TryParseTop(context.Request.Query["$top"], DefaultPageSize);
        var skipToken = context.Request.Query["$skiptoken"].ToString();

        var startIndex = GetStartIndex(orderedDefinitions, skipToken);
        var page = orderedDefinitions
            .Skip(startIndex)
            .Take(pageSize)
            .ToArray();

        string? nextLink = null;
        var hasMore = startIndex + page.Length < orderedDefinitions.Length;
        if (hasMore && page.Length > 0)
        {
            var nextSkipToken = EncodeSkipToken(page[^1].Id);
            nextLink = BuildNextLink(context, nextSkipToken, pageSize);
        }

        var result = new ListSubscriptionRoleDefinitionsResponse
        {
            Value = page.Select(ListSubscriptionRoleDefinitionsResponse.RoleDefinition.From).ToArray(),
            NextLink = nextLink
        };

        response.CreateJsonContentResponse(result);
    }
    
    private static int TryParseTop(string? top, int fallback)
    {
        return int.TryParse(top, out var parsed) && parsed > 0 ? parsed : fallback;
    }

    private static int GetStartIndex(RoleDefinitionResource[] orderedDefinitions, string? skipToken)
    {
        if (string.IsNullOrWhiteSpace(skipToken))
        {
            return 0;
        }

        var lastSeenId = DecodeSkipToken(skipToken);
        if (string.IsNullOrWhiteSpace(lastSeenId))
        {
            return 0;
        }

        var index = Array.FindIndex(
            orderedDefinitions,
            d => string.Equals(d.Id, lastSeenId, StringComparison.OrdinalIgnoreCase));

        return index >= 0 ? index + 1 : 0;
    }

    private static string BuildNextLink(HttpContext context, string skipToken, int pageSize)
    {
        var query = context.Request.Query
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        query["$top"] = pageSize.ToString();
        query["$skiptoken"] = skipToken;

        return
            $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}?{string.Join("&", query.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
    }

    private static string EncodeSkipToken(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static string? DecodeSkipToken(string value)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractRoleNamerFromFilter(string? filter)
    {
        if (string.IsNullOrEmpty(filter)) return null;
        var segments = filter.Split(' ');

        return segments.Length > 1 ? segments[2].Replace("'", string.Empty) : null;
    }
}