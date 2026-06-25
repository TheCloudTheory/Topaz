using Topaz.Chaos.Models;
using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Shared.Extensions;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Chaos.Endpoints.Rules;

internal sealed class GetChaosRuleEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["GET /topaz/chaos/rules/{ruleId}"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ruleId = context.Request.Path.Value!.ExtractValueFromPath(4)!;

        if (!ChaosRulesProvider.TryGet(ruleId, out var rule))
        {
            response.CreateJsonContentResponse(
                new ChaosErrorResponse { Error = new ChaosErrorResponse.ChaosErrorDetail { Code = "RuleNotFound", Message = $"Chaos rule '{ruleId}' was not found." } },
                HttpStatusCode.NotFound);
            return;
        }

        logger.LogDebug(nameof(GetChaosRuleEndpoint), nameof(GetResponse), $"Returning chaos rule '{ruleId}'.");
        response.CreateJsonContentResponse(rule!);
    }
}
