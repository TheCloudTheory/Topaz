using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands.Rules;

[UsedImplicitly]
[CommandDefinition("chaos rule delete", "chaos", "Deletes a chaos fault rule.")]
[CommandExample("Delete a chaos rule", "topaz chaos rule delete --rule-id throttle-storage")]
public sealed class DeleteChaosRuleCommand(HttpClient httpClient)
    : TopazHttpCommand<DeleteChaosRuleCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/topaz/chaos/rules/{settings.RuleId}";
        if (!await DeleteAsync(url)) return 1;
        AnsiConsole.WriteLine($"Chaos rule '{settings.RuleId}' deleted.");
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
        [CommandOptionDefinition("(Required) Rule ID to delete.", required: true)]
        [CommandOption("--rule-id")]
        public string? RuleId { get; set; }
    }
}
