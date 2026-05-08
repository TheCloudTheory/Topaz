using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses.Certificates;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Certificates;

internal sealed class CreateCertificateEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultCertificatesDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";
    public string[] Endpoints => ["POST /certificates/{certName}/create"];
    public string[] Permissions => ["Microsoft.KeyVault/vaults/certificates/create/action"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "create";
    protected override string AccessPolicyScope => "certificates";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var certName = context.Request.Path.Value.ExtractValueFromPath(2);

            if (string.IsNullOrEmpty(certName))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var operation = _dataPlane.CreateCertificate(context.Request.Body,
                vault.GetSubscription(), vault.GetResourceGroup(), vault.Name!, certName);

            if (operation.Result == OperationResult.Failed)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var (bundle, certOperation) = operation.Resource;

            // Azure SDK's StartCreateCertificate polls the operation endpoint;
            // return the operation response so the SDK knows it's already completed.
            response.StatusCode = HttpStatusCode.Accepted;
            response.CreateJsonContentResponse(certOperation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
