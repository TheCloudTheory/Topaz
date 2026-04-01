using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

public sealed class DeleteSecretEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string[] Endpoints => ["DELETE /secrets/{secretName}"];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vaultName = context.Request.Headers["Host"].ToString().Split(".")[0];
            var secretName = context.Request.Path.Value.ExtractValueFromPath(2);
            var identifiers = GlobalDnsEntries.GetEntry(KeyVaultService.UniqueName, vaultName!);

            if (identifiers == null)
            {
                throw new Exception("Identifiers for Azure Key Vault not found.");
            }

            if (string.IsNullOrEmpty(secretName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var subscriptionIdentifier = SubscriptionIdentifier.From(identifiers.Value.subscription);
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(identifiers.Value.resourceGroup);

            var operation = _dataPlane.DeleteSecret(subscriptionIdentifier, resourceGroupIdentifier,
                vaultName!, secretName);

            if (operation.Result == OperationResult.NotFound || operation.Resource == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var content = DeleteSecretResponse.New(operation.Resource.Id, vaultName!, secretName,
                operation.Resource.Attributes);

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
