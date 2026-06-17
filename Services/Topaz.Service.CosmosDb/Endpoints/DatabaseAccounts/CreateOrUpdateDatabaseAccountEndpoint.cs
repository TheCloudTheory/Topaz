using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints.DatabaseAccounts;

internal sealed class CreateOrUpdateDatabaseAccountEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}"
    ];

    public string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateOrUpdateDatabaseAccountEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier =
                SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier =
                ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var accountName = context.Request.Path.Value.ExtractValueFromPath(8);

            if (string.IsNullOrWhiteSpace(accountName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            logger.LogDebug(nameof(CreateOrUpdateDatabaseAccountEndpoint), nameof(GetResponse),
                "Processing payload: {0}", content);

            var request = JsonSerializer.Deserialize<CreateOrUpdateDatabaseAccountRequest>(
                content, GlobalSettings.JsonOptions);
            if (request == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            }

            var result = _controlPlane.CreateOrUpdate(
                subscriptionIdentifier, resourceGroupIdentifier, accountName, request);

            if ((result.Result != OperationResult.Created && result.Result != OperationResult.Updated)
                || result.Resource == null)
            {
                response.CreateErrorResponse(result.Code!, result.Reason!);
                return;
            }

            response.CreateJsonContentResponse(result.Resource, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
