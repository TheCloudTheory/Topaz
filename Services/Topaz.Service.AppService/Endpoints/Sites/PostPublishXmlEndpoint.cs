using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppService.Endpoints.Sites;

internal sealed class PostPublishXmlEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{siteName}/publishxml"
    ];

    public string[] Permissions => ["Microsoft.Web/sites/publishxml/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(PostPublishXmlEndpoint), nameof(GetResponse), "Executing {0}.", nameof(GetResponse));

        try
        {
            var siteName = context.Request.Path.Value.ExtractValueFromPath(8);
            var xml = BuildPublishXml(siteName ?? "site");
            response.Content = new StringContent(xml, Encoding.UTF8, "application/xml");
            response.StatusCode = HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    private static string BuildPublishXml(string siteName) =>
        $"""
        <?xml version="1.0" encoding="utf-8"?>
        <publishData>
          <publishProfile profileName="{siteName} - FTP" publishMethod="FTP"
            publishUrl="ftp://{siteName}.ftp.azurewebsites.topaz.local.dev/site/wwwroot"
            ftpPassiveMode="True"
            userName="{siteName}\\${siteName}"
            userPWD="dummy"
            destinationAppUrl="https://{siteName}.azurewebsites.topaz.local.dev"
            SQLServerDBConnectionString=""
            mySQLDBConnectionString=""
            hostingProviderForumLink=""
            controlPanelLink="https://portal.azure.com"
            webSystem="WebSites" />
        </publishData>
        """;
}
