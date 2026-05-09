using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

internal sealed class UpdateKeyRotationPolicyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["PUT /keys/{keyName}/rotationpolicy"];

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

            if (string.IsNullOrEmpty(keyName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }


            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            var operation = _dataPlane.UpdateKeyRotationPolicy(context.Request.Body, subscriptionIdentifier,
                resourceGroupIdentifier, vaultName, keyName);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Key {keyName} not found.", HttpStatusCode.NotFound);
                return;
            }

            if (operation.Result == OperationResult.BadRequest)
            {
                response.CreateErrorResponse("BadParameter", operation.Reason ?? "Bad request.", HttpStatusCode.BadRequest);
                return;
            }

            response.StatusCode = HttpStatusCode.OK;
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
