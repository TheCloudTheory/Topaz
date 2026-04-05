using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Identity;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Endpoints;

/// <summary>
/// Implements POST /oauth2/token.
///
/// Some Docker clients and SDKs POST a URL-encoded form body instead of passing
/// parameters as query strings. The body may contain:
///   grant_type=refresh_token&amp;refresh_token=&lt;token&gt;&amp;service=&lt;host&gt;&amp;scope=repository:...
///
/// Authentication is required: an ACR refresh token, or valid admin Basic credentials.
/// Unauthenticated requests are rejected with 401.
/// Returns the same <c>access_token</c> / <c>token</c> shape as the GET variant.
/// </summary>
internal sealed class AcrOAuth2PostTokenEndpoint(ContainerRegistryControlPlane controlPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["POST /oauth2/token"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        string body;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            body = reader.ReadToEnd();
        }

        logger.LogDebug(nameof(AcrOAuth2PostTokenEndpoint), nameof(GetResponse),
            "Executing {0}: body={1}", nameof(GetResponse), body);

        var form = ParseForm(body);
        form.TryGetValue("refresh_token", out var refreshToken);

        var objectId = AcrTokenHelper.ResolveObjectId(refreshToken, context, controlPlane, logger);
        if (objectId == null)
        {
            response.CreateJsonContentResponse(
                JsonSerializer.Serialize(
                    new { errors = new[] { new { code = "UNAUTHORIZED", message = "authentication required" } } },
                    GlobalSettings.JsonOptions),
                HttpStatusCode.Unauthorized);
            return;
        }

        var accessToken = JwtHelper.IssueAcrToken(objectId);
        var payload = JsonSerializer.Serialize(
            new { token = accessToken, access_token = accessToken },
            GlobalSettings.JsonOptions);

        response.CreateJsonContentResponse(payload);
    }

    private static Dictionary<string, string> ParseForm(string body)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            result[Uri.UnescapeDataString(part[..idx])] = Uri.UnescapeDataString(part[(idx + 1)..]);
        }
        return result;
    }
}
