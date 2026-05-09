using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses.Secrets;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

internal sealed class GetSecretsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultSecretsDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["GET /secrets", "GET /secrets/"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/secrets/getSecret/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "list";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var vaultName = vault.Name;


            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            var operation = _dataPlane.GetSecrets(subscriptionIdentifier, resourceGroupIdentifier, vaultName!);
            var content = new GetSecretsResponse
            {
                Value = operation.Resource!.Select(s => new GetSecretsResponse.Secret
                {
                    Name = s.Name,
                    Id = s.Id,
                    Attributes = new GetSecretsResponse.Secret.SecretAttributes
                    {
                        Created = s.Attributes!.Created,
                        Enabled = s.Attributes.Enabled,
                        Updated = s.Attributes.Updated
                    },
                    ContentType = s.ContentType
                }).ToArray(),
            };

            response.CreateJsonContentResponse(content);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
