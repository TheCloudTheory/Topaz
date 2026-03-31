using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints;

internal sealed class ListDeletedVaultsEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/deletedVaults"
    ];

    public string[] Permissions => ["Microsoft.KeyVault/locations/deletedVaults/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListDeletedVaultsEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));

            var keyVaults = _controlPlane.ListDeletedBySubscription(subscriptionIdentifier);
            if (keyVaults.result != OperationResult.Success || keyVaults.resource == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = new ListDeletedResponse
            {
                Value = keyVaults.resource.Select(keyVault => new ListDeletedResponse.DeletedKeyVaultResponse
                {
                    Id = $"/subscriptions/{keyVault!.GetSubscription().Value}/providers/Microsoft.KeyVault/locations/{keyVault.Location}/deletedVaults/{keyVault.Name}",
                    Name = keyVault.Name,
                    Properties = new ListDeletedResponse.DeletedKeyVaultResponse.DeletedKeyVaultProperties
                    {
                        VaultId = keyVault.Id,
                        Location = keyVault.Location,
                        DeletionDate = keyVault.DeletionDate,
                        ScheduledPurgeDate = keyVault.ScheduledPurgeDate
                    }
                }).ToArray()
            };

            response.CreateJsonContentResponse(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
