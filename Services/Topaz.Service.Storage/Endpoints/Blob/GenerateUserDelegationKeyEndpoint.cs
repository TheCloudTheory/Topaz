using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Identity;
using Topaz.Service.Shared;
using Topaz.Service.Storage.Models.Requests;
using Topaz.Service.Storage.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.Storage.Endpoints.Blob;

/// <summary>
/// Issues a User Delegation Key for signing User Delegation SAS tokens.
/// Azure REST API: POST /?restype=service&amp;comp=userdelegationkey
///
/// Requires Bearer (OAuth) authentication. SharedKey and unauthenticated callers receive 403,
/// matching Azure Storage behavior exactly. The issued key bytes are derived deterministically
/// from the storage account key and the caller's Entra ID identity so that the same derivation
/// can be used at SAS validation time without persisting the key.
/// </summary>
internal sealed class GenerateUserDelegationKeyEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : BlobDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    public string ProviderNamespace => "Microsoft.Storage";

    public string[] Endpoints => ["POST /?restype=service&comp=userdelegationkey"];

    public string[] Permissions => [];

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        // This operation requires Bearer (OAuth) authentication.
        // SharedKey and unauthenticated callers are rejected with 403, matching Azure behavior.
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
            !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = HttpStatusCode.Forbidden;
            SetStorageErrorContent(response, "AuthenticationFailed",
                "Server failed to authenticate the request. Make sure the value of the Authorization header is formed correctly including the signature.");
            return;
        }

        System.IdentityModel.Tokens.Jwt.JwtSecurityToken? token;
        try
        {
            token = JwtHelper.ValidateJwt(authHeader.ToString());
        }
        catch (Exception ex)
        {
            Logger.LogError(nameof(GenerateUserDelegationKeyEndpoint), nameof(GetResponse),
                "Bearer token validation failed: {0}", ex.Message);
            response.StatusCode = HttpStatusCode.Forbidden;
            SetStorageErrorContent(response, "AuthenticationFailed",
                "Server failed to authenticate the request.");
            return;
        }

        if (token == null)
        {
            response.StatusCode = HttpStatusCode.Forbidden;
            SetStorageErrorContent(response, "AuthenticationFailed",
                "Server failed to authenticate the request.");
            return;
        }

        if (!TryGetStorageAccount(context.Request.Headers, out var storageAccount, out _) || storageAccount == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var body = reader.ReadToEnd();
            var request = GenerateUserDelegationKeyRequest.FromXml(body, Logger);

            if (request == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                SetStorageErrorContent(response, "InvalidXmlDocument",
                    "The specified XML is not syntactically valid.");
                return;
            }

            var oid = token.Subject;
            var tid = token.Claims.FirstOrDefault(c => c.Type == "tid")?.Value ?? string.Empty;

            var version = context.Request.Headers.TryGetValue("x-ms-version", out var xmsVersion)
                ? xmsVersion.ToString()
                : "2020-12-06";

            var keyResponse = UserDelegationKeyResponse.FromRequest(
                request.Start, request.Expiry, oid, tid, version,
                storageAccount.Keys[0].Value);

            var xml = keyResponse.ToXml();
            response.Content = new StringContent(xml, Encoding.UTF8);
            response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }

    private static void SetStorageErrorContent(HttpResponseMessage response, string code, string message)
    {
        var xml = new XDocument(
            new XElement("Error",
                new XElement("Code", code),
                new XElement("Message", message)
            )
        ).ToString();
        response.Content = new StringContent(xml, Encoding.UTF8);
        response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/xml");
    }
}
