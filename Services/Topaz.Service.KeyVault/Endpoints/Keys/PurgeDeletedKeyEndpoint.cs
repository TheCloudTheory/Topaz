using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

internal sealed class PurgeDeletedKeyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["DELETE /deletedkeys/{keyName}"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/purge/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "purge";
    protected override string AccessPolicyScope => "keys";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var vaultName = vault.Name;
            var keyName = context.Request.Path.Value.ExtractValueFromPath(2);

            if (string.IsNullOrEmpty(keyName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }


            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            var operation = _dataPlane.PurgeDeletedKey(subscriptionIdentifier, resourceGroupIdentifier,
                vaultName, keyName);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Deleted key {keyName} not found.", HttpStatusCode.NotFound);
                return;
            }

            response.StatusCode = HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
