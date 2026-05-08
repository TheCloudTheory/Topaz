using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Requests.Keys;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

internal sealed class EncryptKeyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["POST /keys/{keyName}/{keyVersion}/encrypt"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/encrypt/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "encrypt";
    protected override string AccessPolicyScope => "keys";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var vaultName = vault.Name;
            var keyName = context.Request.Path.Value.ExtractValueFromPath(2);
            var keyVersion = context.Request.Path.Value.ExtractValueFromPath(3);

            if (string.IsNullOrEmpty(keyName) || string.IsNullOrEmpty(keyVersion))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }


            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            using var sr = new StreamReader(context.Request.Body);
            var rawBody = sr.ReadToEnd();

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                response.CreateErrorResponse("BadParameter", "Request body is required.", HttpStatusCode.BadRequest);
                return;
            }

            var request = JsonSerializer.Deserialize<KeyOperationRequest>(rawBody, GlobalSettings.JsonOptions);
            if (request == null)
            {
                response.CreateErrorResponse("BadParameter", "Invalid request body.", HttpStatusCode.BadRequest);
                return;
            }

            var operation = _dataPlane.EncryptKey(subscriptionIdentifier, resourceGroupIdentifier,
                vaultName, keyName, keyVersion, request);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    operation.Reason ?? "Key not found.", HttpStatusCode.NotFound);
                return;
            }

            if (operation.Result == OperationResult.Failed)
            {
                response.CreateErrorResponse(operation.Code ?? "BadParameter",
                    operation.Reason ?? "Operation failed.", HttpStatusCode.BadRequest);
                return;
            }

            response.StatusCode = HttpStatusCode.OK;
            response.CreateJsonContentResponse(operation.Resource!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
