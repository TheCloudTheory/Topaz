using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

public sealed class UpdateSecretEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string[] Endpoints => ["PATCH /secrets/{secretName}/{secretVersion}"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/secrets/setSecret/action"];

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
            var secretName = context.Request.Path.Value.ExtractValueFromPath(2);
            var version = context.Request.Path.Value.ExtractValueFromPath(3);

            var vaultOperation = _controlPlane.FindByName(vaultName!);
            if (vaultOperation.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            if (string.IsNullOrEmpty(version))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var subscriptionIdentifier = vaultOperation.Resource!.GetSubscription();
            var resourceGroupIdentifier = vaultOperation.Resource!.GetResourceGroup();

            var operation = _dataPlane.UpdateSecret(context.Request.Body, subscriptionIdentifier,
                resourceGroupIdentifier, vaultName!, secretName!, version);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode, $"Secret {secretName} not found.", HttpStatusCode.NotFound);
                return;
            }

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
