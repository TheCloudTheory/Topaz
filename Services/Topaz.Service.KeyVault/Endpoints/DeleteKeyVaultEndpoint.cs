using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints;

internal sealed class DeleteKeyVaultEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{keyVaultName}"
    ];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(DeleteKeyVaultEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var keyVaultName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(keyVaultName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var existing = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
            switch (existing.Result)
            {
                case OperationResult.NotFound:
                    response.StatusCode = HttpStatusCode.NotFound;
                    return;
                case OperationResult.Failed:
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return;
                default:
                    _controlPlane.Delete(subscriptionIdentifier, resourceGroupIdentifier, keyVaultName);
                    response.StatusCode = HttpStatusCode.OK;
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
