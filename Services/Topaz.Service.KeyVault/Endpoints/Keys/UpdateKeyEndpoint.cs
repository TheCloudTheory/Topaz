using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

internal sealed class UpdateKeyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["PATCH /keys/{keyName}/{keyVersion}"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "update";
    protected override string AccessPolicyScope => "keys";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var vaultName = vault.Name;
            var keyName = context.Request.Path.Value.ExtractValueFromPath(2);
            var version = context.Request.Path.Value.ExtractValueFromPath(3);


            if (string.IsNullOrEmpty(version))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            var operation = _dataPlane.UpdateKey(context.Request.Body, subscriptionIdentifier,
                resourceGroupIdentifier, vaultName!, keyName!, version);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Key {keyName} not found.", HttpStatusCode.NotFound);
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
