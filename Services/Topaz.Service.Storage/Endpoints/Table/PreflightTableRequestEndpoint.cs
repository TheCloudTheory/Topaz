using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Table;

/// <summary>
/// Handles OPTIONS preflight requests for Table Storage CORS validation.
/// See: https://learn.microsoft.com/en-us/rest/api/storageservices/preflight-table-request
/// </summary>
internal sealed class PreflightTableRequestEndpoint(ITopazLogger logger)
    : TableDataPlaneEndpointBase(logger), IEndpointDefinition
{
    public string[] Endpoints => ["OPTIONS /..."];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultTableStoragePort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var origin = context.Request.Headers["Origin"].ToString();
        var requestedMethod = context.Request.Headers["Access-Control-Request-Method"].ToString();

        if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(requestedMethod))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount))
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var subscriptionIdentifier = storageAccount!.GetSubscription();
        var resourceGroupIdentifier = storageAccount!.GetResourceGroup();

        try
        {
            var propertiesOp = ControlPlane.GetTableProperties(subscriptionIdentifier, resourceGroupIdentifier,
                storageAccount.Name);
            var properties = propertiesOp.Resource ?? throw new InvalidOperationException(propertiesOp.Reason);

            if (properties.Cors == null || properties.Cors.Count == 0)
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                return;
            }

            var requestedHeaders = context.Request.Headers["Access-Control-Request-Headers"].ToString();

            foreach (var rule in properties.Cors)
            {
                if (!OriginMatches(rule.AllowedOrigins, origin)) continue;
                if (!MethodMatches(rule.AllowedMethods, requestedMethod)) continue;
                if (!string.IsNullOrEmpty(requestedHeaders) && !HeadersMatch(rule.AllowedHeaders, requestedHeaders)) continue;

                response.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", origin);
                response.Headers.TryAddWithoutValidation("Access-Control-Allow-Methods", rule.AllowedMethods);

                if (!string.IsNullOrEmpty(rule.AllowedHeaders))
                    response.Headers.TryAddWithoutValidation("Access-Control-Allow-Headers", rule.AllowedHeaders);

                if (rule.MaxAgeInSeconds > 0)
                    response.Headers.TryAddWithoutValidation("Access-Control-Max-Age",
                        rule.MaxAgeInSeconds.ToString());

                if (!string.IsNullOrEmpty(rule.ExposedHeaders))
                    response.Headers.TryAddWithoutValidation("Access-Control-Expose-Headers", rule.ExposedHeaders);

                response.Content = new ByteArrayContent([]);
                response.Content.Headers.ContentLength = 0;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.StatusCode = HttpStatusCode.OK;
                return;
            }

            response.StatusCode = HttpStatusCode.Forbidden;
        }
        catch
        {
            response.StatusCode = HttpStatusCode.Forbidden;
        }
    }

    private static bool OriginMatches(string allowedOrigins, string origin)
    {
        if (string.IsNullOrEmpty(allowedOrigins)) return false;
        if (allowedOrigins.Trim() == "*") return true;

        return allowedOrigins.Split(',')
            .Any(o => string.Equals(o.Trim(), origin, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MethodMatches(string allowedMethods, string requestedMethod)
    {
        if (string.IsNullOrEmpty(allowedMethods)) return false;
        if (allowedMethods.Trim() == "*") return true;

        return allowedMethods.Split(',')
            .Any(m => string.Equals(m.Trim(), requestedMethod, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HeadersMatch(string allowedHeaders, string requestedHeaders)
    {
        if (string.IsNullOrEmpty(allowedHeaders)) return false;
        if (allowedHeaders.Trim() == "*") return true;

        var allowed = allowedHeaders.Split(',')
            .Select(h => h.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return requestedHeaders.Split(',')
            .All(h => allowed.Contains(h.Trim()));
    }
}
