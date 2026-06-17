using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.CosmosDb.Endpoints.DatabaseAccounts;

/// <summary>
/// HEAD /providers/Microsoft.DocumentDB/databaseAccountNames/{accountName}
/// Returns 200 if the name already exists (not available), 404 if available.
/// The azurerm Terraform provider calls this before creating an account.
/// </summary>
internal sealed class CheckNameAvailabilityEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly CosmosDbServiceControlPlane _controlPlane = CosmosDbServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.DocumentDB";

    public string[] Endpoints =>
    [
        "HEAD /providers/Microsoft.DocumentDB/databaseAccountNames/{accountName}"
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CheckNameAvailabilityEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        // Path: /providers/Microsoft.DocumentDB/databaseAccountNames/{accountName}
        // indices:  0: ""  1: "providers"  2: "Microsoft.DocumentDB"  3: "databaseAccountNames"  4: accountName
        var accountName = context.Request.Path.Value.ExtractValueFromPath(4);

        var availability = _controlPlane.CheckNameAvailability(accountName ?? string.Empty);

        response.Content = new ByteArrayContent([]);
        // 404 = name is available (does not yet exist); 200 = name is taken.
        response.StatusCode = availability.NameAvailable ? HttpStatusCode.NotFound : HttpStatusCode.OK;
    }
}
