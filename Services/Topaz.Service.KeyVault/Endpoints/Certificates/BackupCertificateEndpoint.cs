using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.KeyVault.Models.Responses.Certificates;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints.Certificates;

internal sealed class BackupCertificateEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultCertificatesDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";
    public string[] Endpoints => ["POST /certificates/{certName}/backup"];
    public string[] Permissions => ["Microsoft.KeyVault/vaults/certificates/backup/action"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "backup";
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

            var operation = _dataPlane.BackupCertificate(
                vault.GetSubscription(), vault.GetResourceGroup(), vault.Name!, certName);

            if (operation.Result == OperationResult.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }

            response.CreateJsonContentResponse(BackupCertificateResponse.New(operation.Resource!));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
