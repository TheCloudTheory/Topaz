using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

internal sealed class RecoverDeletedSecretEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultSecretsDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["POST /deletedsecrets/{secretName}/recover"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/secrets/recover/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "recover";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var vaultName = vault.Name;
            var secretName = context.Request.Path.Value.ExtractValueFromPath(2);

            if (string.IsNullOrEmpty(secretName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }


            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            var operation = _dataPlane.RecoverDeletedSecret(subscriptionIdentifier, resourceGroupIdentifier,
                vaultName!, secretName);

            if (operation.Result == OperationResult.NotFound || operation.Resource == null)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Deleted secret {secretName} not found.", HttpStatusCode.NotFound);
                return;
            }

            response.CreateJsonContentResponse(operation.Resource!);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
