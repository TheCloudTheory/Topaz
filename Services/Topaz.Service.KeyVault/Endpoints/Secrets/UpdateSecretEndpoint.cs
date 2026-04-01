using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

public sealed class UpdateSecretEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string[] Endpoints => ["PATCH /secrets/{secretName}/{secretVersion}"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/secrets/setSecret/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vaultName = context.Request.Headers["Host"].ToString().Split(".")[0];
            var secretName = context.Request.Path.Value.ExtractValueFromPath(2);
            var version = context.Request.Path.Value.ExtractValueFromPath(3);
            var identifiers = GlobalDnsEntries.GetEntry(KeyVaultService.UniqueName, vaultName!);

            if (identifiers == null)
            {
                throw new Exception("Identifiers for Azure Key Vault not found.");
            }

            if (string.IsNullOrEmpty(version))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var subscriptionIdentifier = SubscriptionIdentifier.From(identifiers.Value.subscription);
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(identifiers.Value.resourceGroup);

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
