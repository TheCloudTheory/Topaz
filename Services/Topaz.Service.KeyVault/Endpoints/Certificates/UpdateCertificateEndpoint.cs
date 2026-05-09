using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Certificates;

internal sealed class UpdateCertificateEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultCertificatesDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";
    public string[] Endpoints => ["PATCH /certificates/{certName}/{version}"];
    public string[] Permissions => ["Microsoft.KeyVault/vaults/certificates/update/action"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "update";
    protected override string AccessPolicyScope => "certificates";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var certName = context.Request.Path.Value.ExtractValueFromPath(2);
            var version = context.Request.Path.Value.ExtractValueFromPath(3);

            if (string.IsNullOrEmpty(certName) || string.IsNullOrEmpty(version))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            var operation = _dataPlane.UpdateCertificate(context.Request.Body,
                vault.GetSubscription(), vault.GetResourceGroup(), vault.Name!, certName, version);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Certificate {certName} not found.", HttpStatusCode.NotFound);
                return;
            }

            response.CreateJsonContentResponse(operation.Resource!);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
