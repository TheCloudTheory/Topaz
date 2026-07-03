using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands.Rules;

[UsedImplicitly]
[CommandDefinition("chaos rule enable", "chaos", "Enables a chaos fault rule.")]
[CommandExample("Enable a chaos rule", "topaz chaos rule enable --rule-id throttle-storage")]
public sealed class EnableChaosRuleCommand(HttpClient httpClient)
    : TopazHttpCommand<EnableChaosRuleCommand.Settings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/topaz/chaos/rules/{settings.RuleId}/enable";
        var response = await HttpClient.PostAsync(url, new StreamContent(Stream.Null));
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"Error {(int)response.StatusCode}: {body}");
            return 1;
        }
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
        [CommandOptionDefinition("(Required) Rule ID to enable.", required: true)]
        [CommandOption("--rule-id")]
        public string? RuleId { get; set; }
    }
}
