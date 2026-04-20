using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

public sealed class GetDeletedKeysEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly KeyVaultAuthorizationChecker _authChecker = new(eventPipeline, logger);

    public string[] Endpoints => ["GET /deletedkeys", "GET /deletedkeys/"];

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

            var kvResult = _controlPlane.FindByName(vaultName!);
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

            if (!_authChecker.IsAuthorized(authHeader, kvResult.Resource, Permissions, "list"))
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                return;
            }

            var operation = _dataPlane.GetDeletedKeys(subscriptionIdentifier, resourceGroupIdentifier, vaultName!);
            var content = GetDeletedKeysResponse.FromRecords(operation.Resource!, vaultName!);
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
