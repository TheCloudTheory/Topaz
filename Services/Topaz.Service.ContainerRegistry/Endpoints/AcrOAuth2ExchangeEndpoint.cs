using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Identity;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Endpoints;

/// <summary>
/// Implements POST /oauth2/exchange.
/// Receives an AAD access token and returns an ACR refresh token.
/// This is the first leg of the Docker Registry OAuth2 token flow used by `az acr login`.
/// </summary>
internal sealed class AcrOAuth2ExchangeEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["POST /oauth2/exchange"];

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

        logger.LogDebug(nameof(AcrOAuth2ExchangeEndpoint), nameof(GetResponse), "Exchange body: {0}", body);

        var form = ParseForm(body);

        if (!form.TryGetValue("access_token", out var aadToken) || string.IsNullOrWhiteSpace(aadToken))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent(
                JsonSerializer.Serialize(new { error = "invalid_request", error_description = "access_token is required" },
                    GlobalSettings.JsonOptions));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return;
        }

        var validated = JwtHelper.ValidateJwt(aadToken);
        var objectId = validated?.Subject ?? Globals.GlobalAdminId;

        // Issue an ACR refresh token re-using the same signing infrastructure.
        var refreshToken = JwtHelper.IssueAcrToken(objectId);

        var payload = JsonSerializer.Serialize(
            new { refresh_token = refreshToken },
            GlobalSettings.JsonOptions);

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(payload);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
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
