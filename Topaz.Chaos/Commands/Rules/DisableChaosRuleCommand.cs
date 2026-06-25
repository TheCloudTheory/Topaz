using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Chaos.Commands.Rules;

[UsedImplicitly]
[CommandDefinition("chaos rule disable", "chaos", "Disables a chaos fault rule.")]
[CommandExample("Disable a chaos rule", "topaz chaos rule disable --rule-id throttle-storage")]
public sealed class DisableChaosRuleCommand(HttpClient httpClient)
    : TopazHttpCommand<DisableChaosRuleCommand.Settings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var url = $"{ArmBaseUrl}/topaz/chaos/rules/{settings.RuleId}/disable";
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

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.RuleId))
            return ValidationResult.Error("Rule ID (--rule-id) is required.");
        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class Settings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Rule ID to disable.", required: true)]
        [CommandOption("--rule-id")]
        public string? RuleId { get; set; }
    }
}
