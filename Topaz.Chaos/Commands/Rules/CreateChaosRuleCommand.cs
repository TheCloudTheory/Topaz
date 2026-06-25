using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.Chaos.Models;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands.Rules;

[UsedImplicitly]
[CommandDefinition("chaos rule create", "chaos", "Creates a chaos fault rule.")]
[CommandExample("Create a throttle rule for Storage", "topaz chaos rule create --rule-id throttle-storage --namespace Microsoft.Storage --fault-type Throttle --rate 0.5 --status-code 429")]
public sealed class CreateChaosRuleCommand(HttpClient httpClient)
    : TopazHttpCommand<CreateChaosRuleCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var url = $"{ArmBaseUrl}/topaz/chaos/rules/{settings.RuleId}";
        var request = new CreateChaosRuleRequest
        {
            ServiceNamespace = settings.ServiceNamespace!,
            FaultType = Enum.Parse<FaultType>(settings.FaultType!, ignoreCase: true),
            FaultRate = settings.FaultRate,
            HttpStatusCode = settings.HttpStatusCode
        };
        var (success, body) = await PutAsync(url, request);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.RuleId))
            return ValidationResult.Error("Rule ID (--rule-id) is required.");
        if (string.IsNullOrWhiteSpace(settings.ServiceNamespace))
            return ValidationResult.Error("Service namespace (--namespace) is required.");
        if (string.IsNullOrWhiteSpace(settings.FaultType))
            return ValidationResult.Error("Fault type (--fault-type) is required.");
        if (!Enum.TryParse<FaultType>(settings.FaultType, ignoreCase: true, out _))
            return ValidationResult.Error("Fault type must be one of: Timeout, TransientError, Throttle, ServiceUnavailable.");
        if (settings.FaultRate < 0.0 || settings.FaultRate > 1.0)
            return ValidationResult.Error("Fault rate (--rate) must be between 0.0 and 1.0.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Unique ID for the rule.", required: true)]
        [CommandOption("--rule-id")]
        public string? RuleId { get; set; }

        [CommandOptionDefinition("(Required) Service namespace to match, e.g. Microsoft.Storage or * for all.", required: true)]
        [CommandOption("--namespace")]
        public string? ServiceNamespace { get; set; }

        [CommandOptionDefinition("(Required) Fault type: Timeout | TransientError | Throttle | ServiceUnavailable.", required: true)]
        [CommandOption("--fault-type")]
        public string? FaultType { get; set; }

        [CommandOptionDefinition("(Required) Probability of injecting the fault (0.0–1.0).", required: true)]
        [CommandOption("--rate")]
        public double FaultRate { get; set; }

        [CommandOptionDefinition("(Optional) HTTP status code override (e.g. 429, 500, 503).")]
        [CommandOption("--status-code")]
        public int? HttpStatusCode { get; set; }
    }
}
