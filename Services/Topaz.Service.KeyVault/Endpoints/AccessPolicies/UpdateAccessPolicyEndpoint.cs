using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Requests;
using Topaz.Service.KeyVault.Models.Responses;
using Topaz.Service.ResourceGroup;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.AccessPolicies;

internal sealed class UpdateAccessPolicyEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = new(
        new KeyVaultResourceProvider(logger),
        new ResourceGroupControlPlane(new ResourceGroupResourceProvider(logger),
            new SubscriptionControlPlane(eventPipeline, new SubscriptionResourceProvider(logger)), logger),
        new SubscriptionControlPlane(eventPipeline, new SubscriptionResourceProvider(logger)), logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultName}/accessPolicies/{operationKind}"
    ];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/accessPolicies/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(UpdateAccessPolicyEndpoint), nameof(GetResponse),
            "Executing {0}: {1}", nameof(GetResponse), context.Request.Path.Value);

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
            var vaultName = context.Request.Path.Value.ExtractValueFromPath(8);
            var operationKind = context.Request.Path.Value.ExtractValueFromPath(10);

            if (string.IsNullOrWhiteSpace(vaultName) || string.IsNullOrWhiteSpace(operationKind))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
            var request = JsonSerializer.Deserialize<UpdateAccessPolicyRequest>(content, GlobalSettings.JsonOptions);

            if (request?.Properties == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var result = _controlPlane.UpdateAccessPolicy(
                subscriptionIdentifier, resourceGroupIdentifier, vaultName, operationKind, request);

            if (result.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(result.Code!, result.Reason!);
                return;
            }

            var resource = result.Resource!;
            var accessPoliciesResponse = new VaultAccessPolicyParametersResponse
            {
                Id = $"{resource.Id}/accessPolicies/",
                Name = operationKind,
                Type = "Microsoft.KeyVault/vaults/accessPolicies",
                Properties = new VaultAccessPolicyParametersResponse.VaultAccessPolicyPropertiesResponse
                {
                    AccessPolicies = resource.Properties.AccessPolicies.ToArray()
                }
            };

            response.CreateJsonContentResponse(accessPoliciesResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }
}
