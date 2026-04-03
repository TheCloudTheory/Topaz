using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints.Secrets;

public sealed class RestoreSecretEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);
    private readonly KeyVaultDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string[] Endpoints => ["POST /secrets/restore"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/secrets/restore/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vaultName = context.Request.Headers["Host"].ToString().Split(".")[0];

            var vaultOperation = _controlPlane.FindByName(vaultName!);
            if (vaultOperation.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var subscriptionIdentifier = vaultOperation.Resource!.GetSubscription();
            var resourceGroupIdentifier = vaultOperation.Resource!.GetResourceGroup();

            var operation = _dataPlane.RestoreSecretBackup(context.Request.Body,
                subscriptionIdentifier, resourceGroupIdentifier, vaultName!);

            if (operation.Result == OperationResult.Failed)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            response.CreateJsonContentResponse(operation.Resource!);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.BadRequest;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
