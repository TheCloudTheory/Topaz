using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands.Rules;

[UsedImplicitly]
[CommandDefinition("chaos rule show", "chaos", "Gets a chaos fault rule by ID.")]
[CommandExample("Show a chaos rule", "topaz chaos rule show --rule-id throttle-storage")]
public sealed class GetChaosRuleCommand(HttpClient httpClient)
    : TopazHttpCommand<GetChaosRuleCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/topaz/chaos/rules/{settings.RuleId}";
        var (success, body) = await GetAsync(url);
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.RuleId))
            return ValidationResult.Error("Rule ID (--rule-id) is required.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Rule ID to retrieve.", required: true)]
        [CommandOption("--rule-id")]
        public string? RuleId { get; set; }
    }
}
