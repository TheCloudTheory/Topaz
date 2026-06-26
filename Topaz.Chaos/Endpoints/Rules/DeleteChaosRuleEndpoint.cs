using Topaz.Chaos.Models;
using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Shared.Extensions;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Chaos.Endpoints.Rules;

internal sealed class DeleteChaosRuleEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["DELETE /topaz/chaos/rules/{ruleId}"];
    public string[] Permissions => [];
    public string? ProviderNamespace => "Topaz";
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ruleId = context.Request.Path.Value!.ExtractValueFromPath(4)!;

        if (!ChaosRulesProvider.TryDelete(ruleId))
        {
            response.CreateJsonContentResponse(
                new ChaosErrorResponse { Error = new ChaosErrorResponse.ChaosErrorDetail { Code = "RuleNotFound", Message = $"Chaos rule '{ruleId}' was not found." } },
                HttpStatusCode.NotFound);
            return;
        }

        logger.LogDebug(nameof(DeleteChaosRuleEndpoint), nameof(GetResponse), $"Chaos rule '{ruleId}' deleted.");
        response.StatusCode = HttpStatusCode.NoContent;
    }
}
