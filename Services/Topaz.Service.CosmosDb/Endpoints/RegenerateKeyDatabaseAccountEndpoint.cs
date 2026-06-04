using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.CosmosDb.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints;

internal sealed class RegenerateKeyDatabaseAccountEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}/regenerateKey"
    ];

    public string[] Permissions => ["Microsoft.DocumentDB/databaseAccounts/regenerateKey/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(RegenerateKeyDatabaseAccountEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            var subscriptionIdentifier =
                SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier =
                ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var accountName = context.Request.Path.Value.ExtractValueFromPath(8);

            using var reader = new StreamReader(context.Request.Body);
            var body = reader.ReadToEnd();
            var request = JsonSerializer.Deserialize<RegenerateCosmosDbKeyRequest>(body, GlobalSettings.JsonOptions);

            if (request == null || string.IsNullOrWhiteSpace(request.KeyKind))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent("keyKind is required.");
                return;
            }

            var operation = _controlPlane.RegenerateKey(
                subscriptionIdentifier, resourceGroupIdentifier, accountName!, request.KeyKind);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.NotFound);
                return;
            }

            if (operation.Result == OperationResult.Failed)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Content = new StringContent(operation.Reason ?? "Bad request.");
                return;
            }

            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(string.Empty);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
