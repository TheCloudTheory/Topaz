using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;
using Topaz.CLI.Infrastructure;
using Topaz.Documentation.Command;

namespace Topaz.Service.Subscription.Commands;

[UsedImplicitly]
[CommandDefinition("subscription cancel", "subscription", "Cancels a subscription, setting its state to Disabled.")]
[CommandExample("Cancel a subscription", "topaz subscription cancel \\\n    --id \"6B1F305F-7C41-4E5C-AA94-AB937F2F530A\"")]
public sealed class CancelSubscriptionCommand(HttpClient httpClient)
    : TopazHttpCommand<CancelSubscriptionCommand.CancelSubscriptionCommandSettings>(httpClient)
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancelSubscriptionCommandSettings settings, CancellationToken cancellationToken)
    {
        var url = $"{ArmBaseUrl}/subscriptions/{settings.Id}/providers/Microsoft.Subscription/cancel";
        var (success, body) = await PostAsync(url, new { });
        if (!success) return 1;
        AnsiConsole.WriteLine(body);
        return 0;
    }

    protected override ValidationResult Validate(CommandContext context, CancelSubscriptionCommandSettings settings)
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
    public sealed class CancelSubscriptionCommandSettings : CommandSettings
    {
        [CommandOptionDefinition("(Required) Subscription ID.", required: true)]
        [CommandOption("-i|--id")] public string? Id { get; set; }
    }
}
