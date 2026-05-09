using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses.Keys;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

internal sealed class GetDeletedKeysEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["GET /deletedkeys", "GET /deletedkeys/"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/read"];

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

            var operation = _dataPlane.GetDeletedKeys(subscriptionIdentifier, resourceGroupIdentifier, vaultName!);
            var content = GetDeletedKeysResponse.FromRecords(operation.Resource!, vaultName!);
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
