using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

internal sealed class GetKeysEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["GET /keys", "GET /keys/"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "list";
    protected override string AccessPolicyScope => "keys";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var vaultName = vault.Name;


            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            var operation = _dataPlane.GetKeys(subscriptionIdentifier, resourceGroupIdentifier, vaultName!);

            var content = new GetKeysResponse
            {
                Value = operation.Resource!.Select(b => new GetKeysResponse.KeyItem
                {
                    Kid = b.Key.Kid,
                    Attributes = new GetKeysResponse.KeyItem.KeyItemAttributes
                    {
                        Enabled = b.Attributes.Enabled,
                        Created = b.Attributes.Created,
                        Updated = b.Attributes.Updated
                    },
                    Tags = b.Tags
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
