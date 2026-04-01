using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Dns;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

public sealed class GetSecretsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string[] Endpoints => ["GET /secrets/"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/secrets/getSecret/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vaultName = context.Request.Headers["Host"].ToString().Split(".")[0];
            var identifiers = GlobalDnsEntries.GetEntry(KeyVaultService.UniqueName, vaultName!);

            if (identifiers == null)
            {
                throw new Exception("Identifiers for Azure Key Vault not found.");
            }

            var subscriptionIdentifier = SubscriptionIdentifier.From(identifiers.Value.subscription);
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(identifiers.Value.resourceGroup);

            var operation = _dataPlane.GetSecrets(subscriptionIdentifier, resourceGroupIdentifier, vaultName!);
            var content = new GetSecretsResponse
            {
                Value = operation.Resource!.Select(s => new GetSecretsResponse.Secret
                {
                    Id = s.Id,
                    Attributes = new GetSecretsResponse.Secret.SecretAttributes
                    {
                        Created = s.Attributes.Created,
                        Enabled = s.Attributes.Enabled,
                        Updated = s.Attributes.Updated
                    },
                    ContentType = "plainText" // TODO: Add support for setting this value
                }).ToArray(),
            };

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
