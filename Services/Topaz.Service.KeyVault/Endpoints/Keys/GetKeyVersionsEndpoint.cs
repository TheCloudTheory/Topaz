using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses.Keys;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

internal sealed class GetKeyVersionsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["GET /keys/{keyName}/versions", "GET /keys/{keyName}/versions/"];

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
            var keyName = context.Request.Path.Value.ExtractValueFromPath(2);


            var subscriptionIdentifier = vault.GetSubscription();
            var resourceGroupIdentifier = vault.GetResourceGroup();

            var operation = _dataPlane.GetKeyVersions(subscriptionIdentifier, resourceGroupIdentifier,
                vaultName!, keyName!);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Key {keyName} not found.", HttpStatusCode.NotFound);
                return;
            }

            var content = new GetKeyVersionsResponse
            {
                Value = operation.Resource!.Select(b => new GetKeyVersionsResponse.KeyVersionItem
                {
                    Kid = b.Key.Kid,
                    Attributes = new GetKeyVersionsResponse.KeyVersionItem.KeyVersionAttributes
                    {
                        Enabled = b.Attributes.Enabled,
                        Created = b.Attributes.Created,
                        Updated = b.Attributes.Updated
                    }
                }).ToArray()
            };

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
