using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

public sealed class WrapKeyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly KeyVaultAuthorizationChecker _authChecker = new(eventPipeline, logger);

    public string[] Endpoints => ["POST /keys/{keyName}/{keyVersion}/wrapkey", "POST /keys/{keyName}/wrapkey"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/wrap/action"];

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
            var keyName = context.Request.Path.Value.ExtractValueFromPath(2);
            // Support both /keys/{name}/{version}/wrapkey and /keys/{name}/wrapkey
            var segment3 = context.Request.Path.Value.ExtractValueFromPath(3);
            var keyVersion = string.Equals(segment3, "wrapkey", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : segment3;

            if (string.IsNullOrEmpty(keyName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var kvResult = _controlPlane.FindByName(vaultName);
            if (kvResult.Result == OperationResult.NotFound || kvResult.Resource == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var subscriptionIdentifier = kvResult.Resource.GetSubscription();
            var resourceGroupIdentifier = kvResult.Resource.GetResourceGroup();

            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.Headers.Add("WWW-Authenticate", KeyVaultAuthorizationChecker.WwwAuthenticateChallenge);
                return;
            }

            if (!_authChecker.IsAuthorized(authHeader, kvResult.Resource, Permissions, "wrapKey", "keys"))
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

            var request = JsonSerializer.Deserialize<KeyOperationRequest>(rawBody, GlobalSettings.JsonOptions);
            if (request == null)
            {
                response.CreateErrorResponse("BadParameter", "Invalid request body.", HttpStatusCode.BadRequest);
                return;
            }

            var operation = _dataPlane.WrapKey(subscriptionIdentifier, resourceGroupIdentifier,
                vaultName, keyName, keyVersion, request);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    operation.Reason ?? "Key not found.", HttpStatusCode.NotFound);
                return;
            }

            if (operation.Result == OperationResult.Failed)
            {
                response.CreateErrorResponse(operation.Code ?? "BadParameter",
                    operation.Reason ?? "Operation failed.", HttpStatusCode.BadRequest);
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
