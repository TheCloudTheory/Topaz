using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints;

internal sealed class PurgeDeletedVaultEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/locations/{location}/deletedVaults/{keyVaultName}/purge"
    ];

    public string[] Permissions => ["Microsoft.KeyVault/locations/deletedVaults/purge/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(PurgeDeletedVaultEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var location = context.Request.Path.Value.ExtractValueFromPath(6);
            var keyVaultName = context.Request.Path.Value.ExtractValueFromPath(8);

            var (operationResult, vaultUri) = _controlPlane.Purge(subscriptionIdentifier, location!, keyVaultName!);
            if (operationResult == OperationResult.NotFound || vaultUri == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            response.StatusCode = HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
