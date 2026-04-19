using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

public sealed class DeleteKeyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));
    private readonly KeyVaultAuthorizationChecker _authChecker = new(eventPipeline, logger);

    public string[] Endpoints => ["DELETE /keys/{keyName}"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/delete"];

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

            var vaultOperation = _controlPlane.FindByName(vaultName!);
            if (vaultOperation.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            if (string.IsNullOrEmpty(keyName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var subscriptionIdentifier = vaultOperation.Resource!.GetSubscription();
            var resourceGroupIdentifier = vaultOperation.Resource!.GetResourceGroup();

            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                response.StatusCode = HttpStatusCode.Unauthorized;
                response.Headers.Add("WWW-Authenticate", KeyVaultAuthorizationChecker.WwwAuthenticateChallenge);
                return;
            }

            if (!_authChecker.IsAuthorized(authHeader, vaultOperation.Resource!, Permissions, "delete", "keys"))
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                return;
            }

            var operation = _dataPlane.DeleteKey(subscriptionIdentifier, resourceGroupIdentifier,
                vaultName!, keyName);

            if (operation.Result == OperationResult.NotFound || operation.Resource == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var content = DeleteKeyResponse.New(operation.Resource, vaultName!, keyName);
            response.CreateJsonContentResponse(content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
