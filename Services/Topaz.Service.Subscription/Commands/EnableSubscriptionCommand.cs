using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription enable", "subscription", "Enables a subscription, setting its state to Enabled.")]
[CommandExample("Enable a subscription", "topaz subscription enable \\\n    --id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class EnableSubscriptionCommand(HttpClient httpClient)
    : TopazHttpCommand<EnableSubscriptionCommand.EnableSubscriptionCommandSettings>(httpClient)
{
    public override async Task<int> ExecuteAsync(CommandContext context, EnableSubscriptionCommandSettings settings)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.Id}/providers/Microsoft.Subscription/enable";
        var (success, body) = await PostAsync(url, new { });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, EnableSubscriptionCommandSettings settings)
    {
        if (string.IsNullOrEmpty(settings.Id))
        {
            return ValidationResult.Error("Subscription ID can't be null.");
        }

        if (!Guid.TryParse(settings.Id, out _))
        {
            return ValidationResult.Error("Subscription ID must be a valid GUID.");
        }

        return base.Validate(context, settings);
    }

    [UsedImplicitly]
    public sealed class EnableSubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-i|--id")] public string? Id { get; set; }
    }
}
