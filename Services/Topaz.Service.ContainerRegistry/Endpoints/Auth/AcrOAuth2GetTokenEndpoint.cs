using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Identity;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Endpoints.Auth;

/// <summary>
/// Implements GET /oauth2/token.
///
/// This is the standard Docker Registry V2 token endpoint called by the Docker daemon
/// after receiving a 401 Bearer challenge from GET /v2/. The daemon fetches
///   GET /oauth2/token?service=&lt;host&gt;&amp;scope=repository:&lt;name&gt;:pull,push
///   [&amp;grant_type=refresh_token&amp;refresh_token=&lt;acr_refresh_token&gt;]
/// and expects back an object containing an <c>access_token</c> (and <c>token</c> alias)
/// that it will then include as a Bearer header in subsequent registry requests.
///
/// Authentication is required: an ACR refresh token, or valid admin Basic credentials.
/// Unauthenticated requests are rejected with 401.
/// </summary>
internal sealed class AcrOAuth2GetTokenEndpoint(ContainerRegistryControlPlane controlPlane, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["GET /oauth2/token"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.ContainerRegistryPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(AcrOAuth2GetTokenEndpoint), nameof(GetResponse),
            "Executing {0}: service={1} scope={2} grant_type={3}",
            nameof(GetResponse),
            context.Request.Query["service"].ToString(),
            context.Request.Query["scope"].ToString(),
            context.Request.Query["grant_type"].ToString());

        var refreshToken = context.Request.Query["refresh_token"].ToString();
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
}
