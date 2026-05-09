using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Endpoints.Certificates;

internal sealed class RestoreCertificateEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : KeyVaultDataPlaneEndpointBase(eventPipeline, logger), IEndpointDefinition
{
    private readonly KeyVaultCertificatesDataPlane _dataPlane = new(logger, new KeyVaultResourceProvider(logger));

    public string? ProviderNamespace => "Microsoft.KeyVault";
    public string[] Endpoints => ["POST /certificates/restore"];
    public string[] Permissions => ["Microsoft.KeyVault/vaults/certificates/restore/action"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultKeyVaultPort, GlobalSettings.HttpsPort], Protocol.Https);

    protected override string? AccessPolicyPermission => "restore";
    protected override string AccessPolicyScope => "certificates";

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var vault = GetVault(context);

            var operation = _dataPlane.RestoreCertificateBackup(
                context.Request.Body,
                vault.GetSubscription(), vault.GetResourceGroup(), vault.Name!);

            if (operation.Result == OperationResult.Failed)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return;
            }

            response.CreateJsonContentResponse(operation.Resource!);
        }
        catch (InvalidOperationException ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.BadRequest;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }
    }
}
