using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses.Secrets;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

internal sealed class GetSecretVersionsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultSecretsDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["GET /secrets/{secretName}/versions", "GET /secrets/{secretName}/versions/"];

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
            var secretName = context.Request.Path.Value.ExtractValueFromPath(2);

            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            var operation = _dataPlane.GetSecretVersions(subscriptionIdentifier, resourceGroupIdentifier,
                vaultName!, secretName!);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode, $"Secret {secretName} not found.", HttpStatusCode.NotFound);
                return;
            }

            var content = new GetSecretVersionsResponse
            {
                Value = operation.Resource!.Select(s => new GetSecretVersionsResponse.SecretVersionItem
                {
                    Id = s.Id,
                    ContentType = s.ContentType,
                    Attributes = new GetSecretVersionsResponse.SecretVersionItem.SecretVersionAttributes
                    {
                        Enabled = s.Attributes.Enabled,
                        Created = s.Attributes.Created,
                        Updated = s.Attributes.Updated
                    }
                }).ToArray()
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
