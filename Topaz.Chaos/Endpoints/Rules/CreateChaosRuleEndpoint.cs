using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Shared.Extensions;
using Topaz.Chaos.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Chaos.Endpoints.Rules;

internal sealed class CreateChaosRuleEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["PUT /topaz/chaos/rules/{ruleId}"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var ruleId = context.Request.Path.Value!.ExtractValueFromPath(4)!;
        var request = JsonSerializer.Deserialize<CreateChaosRuleRequest>(context.Request.Body, GlobalSettings.JsonOptions);

        if (request is null)
        {
            response.CreateJsonContentResponse(
                new ChaosErrorResponse { Error = new ChaosErrorResponse.ChaosErrorDetail { Code = "InvalidRequest", Message = "Request body is required." } },
                HttpStatusCode.BadRequest);
            return;
        }

        var rule = new ChaosRule
        {
            Id = ruleId,
            ServiceNamespace = request.ServiceNamespace!,
            FaultType = request.FaultType,
            FaultRate = request.FaultRate,
            HttpStatusCode = request.HttpStatusCode,
            Enabled = true
        };

        if (!ChaosRulesProvider.TryAdd(rule))
        {
            response.CreateJsonContentResponse(
                new ChaosErrorResponse { Error = new ChaosErrorResponse.ChaosErrorDetail { Code = "RuleAlreadyExists", Message = $"A chaos rule with ID '{ruleId}' already exists. Delete the existing rule before creating a new one with the same ID." } },
                HttpStatusCode.BadRequest);
            return;
        }

        logger.LogDebug(nameof(CreateChaosRuleEndpoint), nameof(GetResponse), $"Chaos rule '{ruleId}' created.");
        response.CreateJsonContentResponse(rule, HttpStatusCode.Created);
    }
}
