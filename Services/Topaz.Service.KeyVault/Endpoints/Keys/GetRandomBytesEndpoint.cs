using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Requests.Keys;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Keys;

internal sealed class GetRandomBytesEndpoint(Pipeline eventPipeline, ITopazLogger logger) : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultKeysDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";

    public string[] Endpoints => ["POST /rng"];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/keys/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "rng";
    protected override string AccessPolicyScope => "keys";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var vaultName = vault.Name;


            using var sr = new StreamReader(context.Request.Body);
            var rawBody = sr.ReadToEnd();

            if (string.IsNullOrWhiteSpace(rawBody))
            {
                response.CreateErrorResponse("BadParameter", "Request body is required.", HttpStatusCode.BadRequest);
                return;
            }

            var request = JsonSerializer.Deserialize<GetRandomBytesRequest>(rawBody, GlobalSettings.JsonOptions);
            if (request == null)
            {
                response.CreateErrorResponse("BadParameter", "Invalid request body.", HttpStatusCode.BadRequest);
                return;
            }

            var operation = _dataPlane.GetRandomBytes(request.Count);
            if (operation.Result == OperationResult.Failed)
            {
                response.CreateErrorResponse("BadParameter", operation.Reason ?? "Invalid count.", HttpStatusCode.BadRequest);
                return;
            }

            response.StatusCode = HttpStatusCode.OK;
            response.CreateJsonContentResponse(operation.Resource!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
