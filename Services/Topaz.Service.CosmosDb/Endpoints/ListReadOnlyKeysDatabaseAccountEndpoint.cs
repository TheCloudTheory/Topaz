using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints;

internal sealed class ListReadOnlyKeysDatabaseAccountEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/readonlykeys"
    ];

    public string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/readonlykeys/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListReadOnlyKeysDatabaseAccountEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier =
                SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier =
                ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var accountName = context.Request.Path.Value.ExtractValueFromPath(8);

            var operation = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, accountName!);

            if (operation.Result == OperationResult.NotFound || operation.Resource == null)
            {
                response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.NotFound);
                return;
            }

            var props = operation.Resource.Properties;
            var keys = new DatabaseAccountKeysResponse
            {
                PrimaryReadonlyMasterKey = props?.PrimaryReadonlyMasterKey,
                SecondaryReadonlyMasterKey = props?.SecondaryReadonlyMasterKey
            };

            response.CreateJsonContentResponse(keys);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
