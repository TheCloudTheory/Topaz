using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

public sealed class GetSecretsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string[] Endpoints => ["GET /secrets", "GET /secrets/"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/secrets/getSecret/action"];

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

            var vaultOperation = _controlPlane.FindByName(vaultName!);
            if (vaultOperation.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var subscriptionIdentifier = vaultOperation.Resource!.GetSubscription();
            var resourceGroupIdentifier = vaultOperation.Resource!.GetResourceGroup();

            var operation = _dataPlane.GetSecrets(subscriptionIdentifier, resourceGroupIdentifier, vaultName!);
            var content = new GetSecretsResponse
            {
                Value = operation.Resource!.Select(s => new GetSecretsResponse.Secret
                {
                    Name = s.Name,
                    Id = s.Id,
                    Attributes = new GetSecretsResponse.Secret.SecretAttributes
                    {
                        Created = s.Attributes.Created,
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
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
