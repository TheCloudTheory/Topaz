using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

public sealed class GetRandomBytesEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly KeyVaultAuthorizationChecker _authChecker = new(eventPipeline, logger);

    public string[] Endpoints => ["POST /rng"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var hostSegments = context.Request.Host.Host.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (hostSegments.Length == 0)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var vaultName = PathGuard.SanitizeName(hostSegments[0]);

            var kvResult = _controlPlane.FindByName(vaultName);
            if (kvResult.Result == OperationResult.NotFound || kvResult.Resource == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.Headers.Add("WWW-Authenticate", KeyVaultAuthorizationChecker.WwwAuthenticateChallenge);
                return;
            }

            if (!_authChecker.IsAuthorized(authHeader, kvResult.Resource, Permissions, "rng", "keys"))
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                return;
            }

            using var sr = new StreamReader(context.Request.Body);
            var rawBody = sr.ReadToEnd();

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                response.CreateErrorResponse("BadParameter", "Request body is required.", HttpStatusCode.BadRequest);
                return;
            }

            var request = JsonSerializer.Deserialize<GetRandomBytesRequest>(rawBody, GlobalSettings.JsonOptions);
            if (request == null)
            {
                response.CreateErrorResponse("BadParameter", "Invalid request body.", HttpStatusCode.BadRequest);
                return;
            }

            var operation = _dataPlane.GetRandomBytes(request.Count);
            if (operation.Result == OperationResult.Failed)
            {
                response.CreateErrorResponse("BadParameter", operation.Reason ?? "Invalid count.", HttpStatusCode.BadRequest);
                return;
            }

            response.StatusCode = HttpStatusCode.OK;
            response.CreateJsonContentResponse(operation.Resource!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
