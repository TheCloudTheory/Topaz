using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Certificates;

internal sealed class PurgeDeletedCertificateEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultCertificatesDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";
    public string[] Endpoints => ["DELETE /deletedcertificates/{certName}"];
    public string[] Permissions => ["Microsoft.KeyVault/vaults/certificates/purge/action"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "purge";
    protected override string AccessPolicyScope => "certificates";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);
            var certName = context.Request.Path.Value.ExtractValueFromPath(2);

            if (string.IsNullOrEmpty(certName))
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            var operation = _dataPlane.PurgeDeletedCertificate(
                vault.GetSubscription(), vault.GetResourceGroup(), vault.Name!, certName);

            if (operation.Result == OperationResult.NotFound)
            {
                response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode,
                    $"Deleted certificate {certName} not found.", HttpStatusCode.NotFound);
                return;
            }

            response.StatusCode = HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
